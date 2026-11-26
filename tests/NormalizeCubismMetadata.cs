using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

public static class NormalizeCubismMetadata
{
    static bool Identifier(string name)
    {
        if (String.IsNullOrEmpty(name) || !(Char.IsLetter(name[0]) || name[0]=='_')) return false;
        foreach (char c in name) if (!(Char.IsLetterOrDigit(c) || c=='_')) return false;
        return true;
    }
    static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> root)
    {
        foreach (var type in root) { yield return type; foreach (var child in Types(type.NestedTypes)) yield return child; }
    }
    public static int Main(string[] args)
    {
        var resolver=new DefaultAssemblyResolver(); resolver.AddSearchDirectory(Path.GetDirectoryName(args[0]));
        var module=ModuleDefinition.ReadModule(args[0],new ReaderParameters { AssemblyResolver=resolver });
        var members=new Dictionary<MemberReference,IMemberDefinition>();
        foreach(var reference in module.GetMemberReferences())
        {
            try {
                if(reference is MethodReference) { var value=((MethodReference)reference).Resolve(); if(value!=null&&value.Module==module)members[reference]=value; }
                if(reference is FieldReference) { var value=((FieldReference)reference).Resolve(); if(value!=null&&value.Module==module)members[reference]=value; }
            } catch(AssemblyResolutionException) { }
        }
        var references=new Dictionary<TypeReference,TypeDefinition>();
        foreach(var reference in module.GetTypeReferences()) { try { var type=reference.Resolve();if(type!=null&&type.Module==module)references[reference]=type; } catch(AssemblyResolutionException){} }
        int renamed=0;
        foreach(var type in Types(module.Types))
        {
            if(type.Name=="<Module>")continue;
            if(!Identifier(type.Name)||(!type.IsPublic&&!type.IsNestedPublic)) { type.Name="CubismType_"+type.MetadataToken.RID;renamed++; }
            foreach(var field in type.Fields)if(!field.IsRuntimeSpecialName&&(!Identifier(field.Name)||!field.IsPublic)) {field.Name="CubismField_"+field.MetadataToken.RID;renamed++;}
            foreach(var method in type.Methods)
            {
                // Preserve method names and virtual slots. AST escaping handles their spelling.
                for(int i=0;i<method.Parameters.Count;i++) method.Parameters[i].Name="argument"+i;
                for(int i=0;i<method.GenericParameters.Count;i++)method.GenericParameters[i].Name="MethodTypeArgument"+i;
            }
            foreach(var property in type.Properties)if(!Identifier(property.Name)) {property.Name="CubismProperty_"+property.MetadataToken.RID;renamed++;}
            foreach(var ev in type.Events)if(!Identifier(ev.Name)) {ev.Name="CubismEvent_"+ev.MetadataToken.RID;renamed++;}
            for(int i=0;i<type.GenericParameters.Count;i++)type.GenericParameters[i].Name="TypeArgument"+i;
        }
        foreach(var pair in members)pair.Key.Name=pair.Value.Name;
        foreach(var pair in references){pair.Key.Name=pair.Value.Name;pair.Key.Namespace=pair.Value.Namespace;}
        module.Write(args[1]);module.Dispose();
        Console.WriteLine("Normalized "+renamed+" metadata identifiers in a local working copy; IL instructions and original assembly untouched.");
        return 0;
    }
}

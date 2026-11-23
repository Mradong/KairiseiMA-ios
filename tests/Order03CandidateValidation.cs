using System;
using System.IO;
using Kairisei.Recovery;
class Order03CandidateValidation {
 static int Main(string[] args){try{var bytes=File.ReadAllBytes(args[0]);CombatTableSet.Validate(bytes);Console.WriteLine("validator=PASS bytes="+bytes.Length);return 0;}catch(Exception e){Console.Error.WriteLine(e.GetType().Name);return 1;}}
}

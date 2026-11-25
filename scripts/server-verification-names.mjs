import fs from 'node:fs';
import path from 'node:path';

export function lydFilename(filename) {
  const extension=path.extname(filename);
  const stem=filename.slice(0,filename.length-extension.length);
  return stem.endsWith('-lyd')?filename:stem+'-lyd'+extension;
}

export function labelVerificationFiles(directory) {
  const files=[];
  function walk(base) {
    for(const entry of fs.readdirSync(base,{withFileTypes:true})) {
      const filename=path.join(base,entry.name);
      if(entry.isDirectory())walk(filename);
      else if(entry.isFile())files.push(filename);
      else throw new Error('Unexpected verification-package entry');
    }
  }
  walk(directory);
  const names=new Map(files.map(file=>[path.basename(file),lydFilename(path.basename(file))]));
  for(const filename of files) {
    const destination=path.join(path.dirname(filename),lydFilename(path.basename(filename)));
    if(destination!==filename&&fs.existsSync(destination))throw new Error('Verification filename collision');
    if(['.ps1','.cmd','.md','.txt','.json'].includes(path.extname(filename).toLowerCase())) {
      let content=fs.readFileSync(filename,'utf8');
      for(const [original,labelled] of names)content=content.replaceAll(original,labelled);
      const outputAssignment='$outputPath = [IO.Path]::GetFullPath($Output)';
      if(content.includes(outputAssignment)) {
        content=content.replace(outputAssignment,outputAssignment+"\n$outputExtension = [IO.Path]::GetExtension($outputPath)\n$outputStem = [IO.Path]::GetFileNameWithoutExtension($outputPath)\nif (!$outputStem.EndsWith('-lyd')) { $outputPath = Join-Path ([IO.Path]::GetDirectoryName($outputPath)) ($outputStem + '-lyd' + $outputExtension) }");
      }
      fs.writeFileSync(filename,content);
    }
    if(destination!==filename)fs.renameSync(filename,destination);
  }
}

import path from 'node:path';
import readline from 'node:readline';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
export class Order03Engine {
  constructor() {
    const tables=path.join(root,'inputs/server-order03/Order03-Combat-Verified-20261123/tables');
    this.process=spawn(path.join(root,'test-output/order03-live-engine/engine.exe'),[path.join(tables,'card.csv'),tables],{windowsHide:true,stdio:['pipe','pipe','pipe']});
    this.waiting=null;
    this.failure=null;
    const lines=readline.createInterface({input:this.process.stdout});
    lines.on('line',line=>{
      const waiting=this.waiting;
      this.waiting=null;
      if(!waiting)return;
      clearTimeout(waiting.timer);
      try { const result=JSON.parse(line); if(!result.success)throw new Error(result.stage+' '+(result.error||''));waiting.resolve(result); }
      catch(error){waiting.reject(error);}
    });
    this.process.on('error',error=>this.fail(error));
    this.process.on('exit',code=>this.fail(new Error('engine exited '+code)));
    this.process.stderr.on('data',()=>{});
  }
  fail(error) {
    this.failure=error;
    if(this.waiting){clearTimeout(this.waiting.timer);this.waiting.reject(error);this.waiting=null;}
  }
  request(command) {
    if(this.failure)return Promise.reject(this.failure);
    if(this.waiting)return Promise.reject(new Error('concurrent engine request'));
    return new Promise((resolve,reject)=>{
      const timer=setTimeout(()=>{this.waiting=null;reject(new Error('engine timeout'));},60000);
      this.waiting={resolve,reject,timer};
      this.process.stdin.write(JSON.stringify(command)+'\n');
    });
  }
  close(){this.process.stdin.end();}
}

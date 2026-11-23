import net from 'node:net';
const pipe = process.argv[2];
const socket = net.createConnection(`\\\\.\\pipe\\${pipe}`);
const deadline = setTimeout(() => { console.error('Interop timeout'); process.exitCode = 1; socket.destroy(); }, 5000);
let buffer = '', event = false;
socket.setEncoding('utf8');
socket.on('connect', () => socket.write('{"type":"hello","version":1}\n'));
socket.on('data', data => {
 buffer += data;
 let n;
 while ((n = buffer.indexOf('\n')) >= 0) {
  const message = JSON.parse(buffer.slice(0,n)); buffer = buffer.slice(n+1);
  if(message.type === 'hello') socket.write('{"type":"command","id":"node-1","command":"annotation.setColor","arguments":{"color":"#778899"}}\n');
  if(message.type === 'state') event = true;
  if(message.type === 'result') {
   if(!message.ok || !event || message.state.annotationColor !== '#778899') process.exitCode = 1;
   else console.log('Node/.NET pipe interoperability PASS');
   clearTimeout(deadline); socket.end();
  }
 }
});
socket.on('error', () => { clearTimeout(deadline); process.exitCode=1; });

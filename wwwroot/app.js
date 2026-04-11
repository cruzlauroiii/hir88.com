const DB_NAME='hir88',DB_VER=1,STORES=['jobs','applications','saved','profile','alerts'];
let db,ws,wsReady=false;

function openDb(){return new Promise((ok,fail)=>{const r=indexedDB.open(DB_NAME,DB_VER);r.onupgradeneeded=e=>{const d=e.target.result;STORES.forEach(s=>{if(!d.objectStoreNames.contains(s))d.createObjectStore(s,{keyPath:'key'})})};r.onsuccess=e=>{db=e.target.result;ok(db)};r.onerror=e=>fail(e)})}

function tx(store,mode='readonly'){return db.transaction(store,mode).objectStore(store)}
function dbPut(store,key,data){return new Promise(ok=>{data.key=key;const r=tx(store,'readwrite').put(data);r.onsuccess=()=>{syncPut(store,key,data);ok()}})}
function dbGet(store,key){return new Promise(ok=>{const r=tx(store).get(key);r.onsuccess=()=>ok(r.result||null)})}
function dbGetAll(store){return new Promise(ok=>{const r=tx(store).getAll();r.onsuccess=()=>ok(r.result||[])})}
function dbDelete(store,key){return new Promise(ok=>{const r=tx(store,'readwrite').delete(key);r.onsuccess=()=>{syncDelete(store,key);ok()}})}

function connectWs(){
    try{
        const proto=location.protocol==='https:'?'wss:':'ws:';
        ws=new WebSocket(proto+'//'+location.host+'/ws');
        ws.onopen=()=>{wsReady=true;STORES.forEach(s=>ws.send(JSON.stringify({type:'sync',store:s})))};
        ws.onmessage=e=>{const m=JSON.parse(e.data);if(m.type==='sync'&&m.items){const t=db.transaction(m.store,'readwrite'),s=t.objectStore(m.store);m.items.forEach(i=>s.put(i))}};
        ws.onclose=()=>{wsReady=false};
        ws.onerror=()=>{wsReady=false;try{ws.close()}catch(e){}}
    }catch(e){wsReady=false}
}
function syncPut(store,key,data){if(wsReady)ws.send(JSON.stringify({type:'put',store,key,data}))}
function syncDelete(store,key){if(wsReady)ws.send(JSON.stringify({type:'delete',store,key}))}

function uid(){return Date.now().toString(36)+Math.random().toString(36).slice(2,8)}
function ago(d){const ms=Date.now()-new Date(d).getTime(),h=ms/36e5;if(h<1)return'Just now';if(h<24)return Math.floor(h)+'h ago';return Math.floor(h/24)+'d ago'}
function esc(s){const d=document.createElement('div');d.textContent=s;return d.innerHTML}

async function seedJobs(){
    const existing=await dbGetAll('jobs');
    if(existing.length>0)return;
    const jobs=[
        {key:uid(),title:'Senior Full Stack Developer',company:'TechVault Inc.',location:'Manila, Philippines',type:'Full-time',salary:'PHP 80,000 - 120,000',desc:'Build scalable web apps with .NET and React. Lead a team of 5 developers. Microservices architecture, Azure cloud, CI/CD pipelines.',tags:['C#','.NET','React','Azure','SQL'],remote:true,visa:false,posted:new Date(Date.now()-864e5).toISOString()},
        {key:uid(),title:'AI Platform Engineer',company:'DataMind AI',location:'Singapore',type:'Full-time',salary:'SGD 8,000 - 12,000',desc:'Design and deploy ML pipelines. Work with LLMs, vector databases, and real-time inference.',tags:['Python','ML','Kubernetes','Docker'],remote:true,visa:true,posted:new Date(Date.now()-1728e5).toISOString()},
        {key:uid(),title:'Frontend Developer',company:'PixelCraft Studio',location:'Cebu, Philippines',type:'Full-time',salary:'PHP 50,000 - 75,000',desc:'Build responsive UIs with React and TypeScript. Component library. Performance optimization.',tags:['React','TypeScript','CSS','Figma'],remote:false,visa:false,posted:new Date(Date.now()-2592e5).toISOString()},
        {key:uid(),title:'DevOps Engineer',company:'CloudFirst Solutions',location:'Remote',type:'Full-time',salary:'USD 5,000 - 8,000',desc:'Manage AWS infrastructure. Terraform, Docker, Kubernetes. On-call rotation. 99.99% uptime SLA.',tags:['AWS','Terraform','Docker','Kubernetes'],remote:true,visa:false,posted:new Date(Date.now()-864e5).toISOString()},
        {key:uid(),title:'Mobile Developer',company:'AppForge PH',location:'Makati, Philippines',type:'Full-time',salary:'PHP 60,000 - 90,000',desc:'Cross-platform mobile apps with Flutter. REST API integration. Offline-first architecture.',tags:['Flutter','Dart','Firebase','REST'],remote:false,visa:false,posted:new Date(Date.now()-3456e5).toISOString()},
        {key:uid(),title:'Backend Developer - .NET',company:'FinServ Global',location:'BGC, Philippines',type:'Full-time',salary:'PHP 70,000 - 100,000',desc:'Financial services APIs. C# .NET 8, SQL Server, message queues. High-throughput transaction processing.',tags:['C#','.NET','SQL Server','RabbitMQ'],remote:false,visa:false,posted:new Date(Date.now()-1728e5).toISOString()},
        {key:uid(),title:'Data Engineer',company:'InsightHub Analytics',location:'Remote',type:'Contract',salary:'USD 4,000 - 6,000',desc:'ETL pipelines with Spark and Airflow. Data warehouse design. dbt, Snowflake, Python.',tags:['Python','Spark','Airflow','SQL'],remote:true,visa:false,posted:new Date(Date.now()-432e6).toISOString()},
        {key:uid(),title:'Cloud Architect',company:'SkyBridge Tech',location:'Singapore',type:'Full-time',salary:'SGD 12,000 - 18,000',desc:'Multi-cloud strategy. AWS, Azure, GCP. Cost optimization. Security architecture.',tags:['AWS','Azure','GCP','Architecture'],remote:true,visa:true,posted:new Date(Date.now()-864e5).toISOString()},
        {key:uid(),title:'QA Automation Engineer',company:'TestPro Systems',location:'Ortigas, Philippines',type:'Full-time',salary:'PHP 45,000 - 65,000',desc:'Selenium, Playwright, Cypress. API testing. CI/CD integration.',tags:['Selenium','Playwright','C#','Testing'],remote:false,visa:false,posted:new Date(Date.now()-2592e5).toISOString()},
        {key:uid(),title:'Site Reliability Engineer',company:'Uptime Global',location:'Remote',type:'Full-time',salary:'USD 7,000 - 11,000',desc:'Observability stack. Prometheus, Grafana. Incident management. Chaos engineering.',tags:['SRE','Kubernetes','Prometheus','Go'],remote:true,visa:true,posted:new Date(Date.now()-2592e5).toISOString()},
        {key:uid(),title:'Cybersecurity Analyst',company:'SecureNet PH',location:'Manila, Philippines',type:'Full-time',salary:'PHP 55,000 - 80,000',desc:'SIEM monitoring, incident response, vulnerability assessment. SOC operations.',tags:['Security','SIEM','Networking','Linux'],remote:false,visa:false,posted:new Date(Date.now()-5184e5).toISOString()},
        {key:uid(),title:'Product Manager - Tech',company:'LaunchPad Inc.',location:'Makati, Philippines',type:'Full-time',salary:'PHP 90,000 - 130,000',desc:'Own product roadmap. Agile/Scrum. Data-driven decisions. User research.',tags:['Product','Agile','Analytics','UX'],remote:false,visa:false,posted:new Date(Date.now()-1728e5).toISOString()},
    ];
    for(const j of jobs)await dbPut('jobs',j.key,j);
}

window.hir88={openDb,dbPut,dbGet,dbGetAll,dbDelete,connectWs,seedJobs,uid,ago,esc};

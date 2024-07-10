const apiPrefix = "/api";
axios.defaults.withCredentials = true
async function save(data) {
    try {
        const res = await axios.post(apiPrefix + "/schemas", encode(data))
        return res.data;
    } catch (err) {
        console.error('POST request error:', err);
    }
}

function encode(data){
    data = removeEmptyProperties(data);
    console.log({data})
    return {
        id : data.id,
        name: data.name,
        type: data.type,
        settings: {
            [data.type]: data,
        },
    } 
}

async function list(){
    const res= await  axios.get(apiPrefix + "/schemas")
    return res.data
}
async function saveDefine(data){
    
    const res= await  axios.post(apiPrefix + `/schemas/define`, encode(data));
    return res.data;
}

async function define(name){
    const res= await  axios.get(apiPrefix + `/schemas/${name}/define`)
    return res.data
}

async function one(id){
    const res= await  axios.get(apiPrefix + `/schemas/${id}`)
    const data = res.data;
    data.settings[data.type].name = data.name
    return data.settings[data.type]
}

async function del(id){
    const res= await  axios.delete(apiPrefix + `/schemas/${id}`)
    return res.data
}

function removeEmptyProperties(obj) {
    for (let key in obj) {
        if (obj.hasOwnProperty(key)) {
            if (typeof obj[key] === 'object') {
                removeEmptyProperties(obj[key]);
                if (Object.keys(obj[key]).length === 0) {
                    delete obj[key];
                }
            } else if (obj[key] === '') {
                delete obj[key];
            }else if (obj[key] === null|| obj[key] === undefined){
                delete obj[key];
            }
        }
    }
    return obj;
}

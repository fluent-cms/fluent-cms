const apiPrefix = "/api";
axios.defaults.withCredentials = true
async function save(data){
    try{
        data = removeEmptyProperties(data)
        const res = await axios.post(apiPrefix + "/schemas", removeEmptyProperties(data))
        return res.data
    }catch (err){
        console.error('POST request error:', err);
    }
}

async function list(){
    const res= await  axios.get(apiPrefix + "/schemas")
    return res.data
}
async function saveDefine(data){
    const res= await  axios.post(apiPrefix + `/schemas/define`, removeEmptyProperties(data))
    return res.data
}

async function define(id){
    const res= await  axios.get(apiPrefix + `/schemas/${id}/define`)
    return res.data
}

async function one(id){
    const res= await  axios.get(apiPrefix + `/schemas/${id}`)
    return res.data
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

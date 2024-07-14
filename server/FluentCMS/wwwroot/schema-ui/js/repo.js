const apiPrefix = "/api";
axios.defaults.withCredentials = true
async function save(data) {
    return tryFetch(async ()=>await axios.post(apiPrefix + "/schemas", encode(data)))
}
async function list(type){
    return  await tryFetch(async ()=>await  axios.get(apiPrefix + `/schemas?type=${type??''}`))
}
async function saveDefine(data){
    return await tryFetch(async ()=>await  axios.post(apiPrefix + `/schemas/define`, encode(data)))
}

async function define(name){
    return await tryFetch(async ()=> await  axios.get(apiPrefix + `/schemas/${name}/define`))
}

async function one(id){
    const {data, err} = await tryFetch(async ()=>await  axios.get(apiPrefix + `/schemas/${id}`))
    if (err){
        return {err}
    }
    
    data.settings[data.type].name = data.name;
    data.settings[data.type].id = data.id;
    return {data: data.settings[data.type]}
}

async function del(id){
    return tryFetch(async ()=> await axios.delete(apiPrefix + `/schemas/${id}`));
}
async function tryFetch(cb){
    try {
        const res = await cb()
        return {data:res.data};
    }catch (err){
        return {error: err.response.data.title??'An error has occurred. Please try again.'}
    }
}
function encode(data){
    data = removeEmptyProperties(data);
    return {
        id : data.id,
        name: data.name,
        type: data.type,
        settings: {
            [data.type]: data,
        },
    }
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

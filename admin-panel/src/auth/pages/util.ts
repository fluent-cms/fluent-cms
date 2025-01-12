export function arrayToCvs(data:any, columns:string[]) {
    if (!data) {
        return null;
    }

    const item = {...data}
    columns.forEach(x => {
        if (Array.isArray(data[x]) && data[x].length > 0) {
            item[x] = data[x].join(',')
        }
    });
    return item;
}

export function cvsToArray(formData:any, columns:string[]){
    const item = {...formData||{}}
    columns.forEach(x =>{
        if (typeof formData[x] === 'string') {
            item[x] = formData[x]?.split(',');
        }
    })
    return item;
}
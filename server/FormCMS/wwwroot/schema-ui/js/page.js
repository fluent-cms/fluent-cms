import {loadEditor} from "../grapes-components/grapes.js";

let id = new URLSearchParams(window.location.search).get("id");
$(document).ready(function() {
    let editor
    getUserInfo().then(({data,error})=>
    {
        if (error){
            window.location.href = "/admin?ref=" + encodeURIComponent(window.location.href);
            return;
        }
        editor = loadEditor("#gjs", loadData);
    });

    $('#visitPage').on('click', function () {
        const name = $(`#name`).val();
        if (name){
            window.open(`/${name}`, '_blank'); // Opens in a new tab
        }
    });
    
    $('#savePage').on('click', async function() {
        
        const payload = {
            html: editor.getHtml(),
            css: editor.getCss(),
            components:JSON.stringify(editor.getComponents()),
            styles: JSON.stringify(editor.getStyle()),
            type: 'page',
        };
        
        if (id) {
            payload.id = +id;
        }
        if (!collectFormData(payload)){
            return false;
        }
        
        $.LoadingOverlay("show");
        const {data, error} = await save(payload);
        if (data) {
            $.toast({
                heading: 'Success',
                text: 'submit succeed!',
                showHideTransition: 'slide',
                icon: 'success'
            })
            if (!id) {
                window.location.href = `page.html?id=${data.id}`;
            }
            $('#errorPanel').text('').hide();
        } else {
            $('#errorPanel').text(error).show();
        }
        $.LoadingOverlay("hide"); 
    });
});

const controls = ['name','title', 'query', 'queryString'];

async function loadData(editor)  {
   
    if (id) {
        $.LoadingOverlay("show");
        const {data, error} = await one(id);
        $.LoadingOverlay("hide");
        if (data){
            $('title').text(`🏠${data.name} - page setting - Fluent CMS Schema Builder`);
            id = data.id;
            restoreFormData(data);
            editor.setComponents(JSON.parse(data.components));
            editor.setStyle(JSON.parse(data.styles));
        }else {
            $('#errorPanel').text(error).show();
        }
    }
}

function restoreFormData(payload){
    for (const ctl of controls){
       $(`#${ctl}`).val(payload[ctl]);
    }
}

function collectFormData(payload){
    for (const ctlName of controls){
        const ctl = $(`#${ctlName}`);
        if (ctl.prop('required') && !ctl.val()){
            $('#errorPanel').text(`${ctlName} can not be empty`).show();
            return false;
        }
        payload[ctlName] = ctl.val();
    }
    return true;
}
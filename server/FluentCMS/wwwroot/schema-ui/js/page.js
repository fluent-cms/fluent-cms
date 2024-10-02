import {loadEditor} from "../grapes-components/grapes.js";
$(document).ready(function() {
    const editor = loadEditor("#gjs");

    const searchParams = new URLSearchParams(window.location.search);
    let id = searchParams.get("id");
    
    editor.on('load',async () => {
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
    });

    $('#visitPage').on('click', function () {
        const name = $(`#name`).val();
        if (name){
            window.open(`/pages/${name}`, '_blank'); // Opens in a new tab
        }
    });
    
    $('#savePage').on('click', async function() {
        console.log(editor.getHtml());
        
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
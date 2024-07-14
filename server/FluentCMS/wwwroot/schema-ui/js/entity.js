$(document).ready(function (){
    $('#saveEntity').on('click', function() {
        submit(save);
    });
    $('#define').on('click',async function() {
        const oldValue = editor.getValue();
        const tableName = oldValue.tableName;
        $.LoadingOverlay("show");
        const {data, error} = await define(tableName);
        if (data){
            oldValue.attributes = data.attributes;
            editor.setValue(oldValue);
        }else {
            $('#errorPanel').text(error).show();
        }
        $.LoadingOverlay("hide");
    });

    $('#saveDefine').on('click', function() {
        submit(saveDefine);
    });

    $('#editContent').on('click', function() {
        const val = editor.getValue();
        window.location.href=`/entities/${val.name}`;
    });   
});

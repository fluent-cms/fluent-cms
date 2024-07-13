$(document).ready(function() {
    const searchParams = new URLSearchParams(window.location.search);
    const schema = searchParams.get("schema");
    let id = searchParams.get("id");
    const editor = loadEditor();

    if (schema === "entity") {
        $('#viewActions').prop('hidden', true);
        $('#menuActions').prop('hidden', true);
        $('#advancedEntityActions').hide();
        if (!id){
            $('#editContent').prop('hidden', true);
        }
    }else if (schema ==="view"){
        $("#entityActions").prop('hidden', true);
        $("#menuActions").prop('hidden', true);
    }else if (schema ==="menu"){
        $("#viewActions").prop('hidden', true);
        $("#entityActions").prop('hidden', true);
    }

    $('#showAdvancedActions').change(function() {
        if ($(this).is(':checked')) {
            $('#advancedEntityActions').show();
        } else {
            $('#advancedEntityActions').hide();
        }
    });
    
    $('#saveEntity').on('click', function() {
        submit(save);
    });
    $('#saveView').on('click', function() {
        submit(save);
    });
    $('#saveMenu').on('click', function() {
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
    $('#viewAPI').on('click', function() {
        const val = editor.getValue();
        window.location.href=`/api/views/${val.name}`;
    });
    
    
    
    async function submit(callback) {
        const errors = editor.validate();
        if (errors.length) {
            return;
        }

        const val = editor.getValue();
        val.type = schema;

        if (id) {
            val.id = +id;
        }

        $.LoadingOverlay("show");
        const {data, error} = await callback(val);
        if (data) {
            alert("submit succeed!");
            if (!id) {
                window.location.href = `edit.html?schema=${schema}&id=${data.id}`;
            }
            $('#errorPanel').text('').hide();
        } else {
            $('#errorPanel').text(error).show();
        }
        $.LoadingOverlay("hide");
    }

    function loadEditor() {
        let editor = new JSONEditor($('#editor_holder')[0], {
            ajax: true,
            schema: {
                "$ref": schema + ".json",
            },
            compact: true,
            disable_collapse: true,
            disable_array_delete_last_row: true,
            disable_array_delete_all_rows: true,
            disable_properties: true,
            disable_edit_json: true,
            collapsed: true,
            object_layout: "grid",
            show_errors: 'always'
        });

        editor.on('ready',async function() {
            if (id) {
                $.LoadingOverlay("show");
                const {data, error} = await one(id);
                if (data){
                    id = data.id;
                    delete (data.id);
                    editor.setValue(data);
                }else {
                    $('#errorPanel').text(error).show();
                }
                $.LoadingOverlay("hide");
            } else {
                editor.setValue(null);
            }
        });

        editor.on('change', function() {
            let errors = editor.validate();
            let indicator = $('#valid_indicator');
            if (errors.length) {
                indicator.css('color', 'red').text("not valid");
            } else {
                indicator.css('color', 'green').text("valid");
            }
        });
        return editor;
    }
});
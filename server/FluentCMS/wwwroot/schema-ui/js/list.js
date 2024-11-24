$(document).ready(function() {
    const searchParams = new URLSearchParams(window.location.search);
    const schema = searchParams.get("schema");
    $('#addEntity').prop('hidden', schema !=='entity');
    $('#addPage').prop('hidden', schema !=='page');
    $('#addQuery').prop('hidden', schema !=='query');
    
    if (schema){
        $('title').text(`${schema} list - Fluent CMS Schema Builder`);
    }
    getUserInfo().then(({data,error})=>
    {
        if (error){
            console.log(error)
            window.location.href = "/admin?ref=/schema";
        }else {
            renderTable();
        }
    });
    async function deleteSchema(e) {
        if (confirm("Do you want to delete schema: " + e.getAttribute('data-name'))) {
            $.LoadingOverlay("show");
            const { error} = await del(e.id);
            $.LoadingOverlay("hide");
            if (error){
                $('#errorPanel').text(error).show();
            }else {
                window.location.reload();
            }
        }
    }

    async function renderTable() {
        const elementId = 'table-body';
        $.LoadingOverlay("show");
        const {data, error} = await list(schema);

        if (data) {
            const $tableBody = $('#' + elementId);
            data.forEach(item => {
                const $row = $('<tr></tr>');
                let url = ""
                switch (item.type){
                    case 'page':
                        url = `page.html?schema=${item.type}&id=${item.id}`;
                        break;
                    case 'query':
                        url = item.settings.query.ideUrl;
                        break;
                    default:
                        url = `edit.html?schema=${item.type}&id=${item.id}`;
                        break;
                }
                $row.html(`
                        <td>${item.id}</td>
                        <td><a href="${url}">${item.name}</a></td>
                        <td>${item.type}</td>
                        <td><button class="btn badge btn-danger btn-sm delete-btn" id="${item.id}" data-name="${item.name}">Delete</button></td>
                    `);

                $tableBody.append($row);
            });
            // Attach click event handler after rows are appended
            $('.delete-btn').click(function() {
                deleteSchema(this);
            });
        } else {
            $('#errorPanel').text(error).show();
        }
        $.LoadingOverlay("hide");
    }
    
});
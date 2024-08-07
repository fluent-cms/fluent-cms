$(document).ready(function() {
    const searchParams = new URLSearchParams(window.location.search);
    const schema = searchParams.get("schema");
    if (schema === "view") {
        $('#addEntity').prop('hidden', true);
    }
    if (schema === "entity") {
        $('#addView').prop('hidden', true);
    }
    async function deleteSchema(e) {
        if (confirm("Do you want to delete schema: " + e.getAttribute('data-name'))) {
            $.LoadingOverlay("show");
            await del(e.id);
            $.LoadingOverlay("hide");
            window.location.reload();
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
                $row.html(`
                        <td>${item.id}</td>
                        <td><a href="edit.html?schema=${item.type}&id=${item.id}">${item.name}</a></td>
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


    renderTable();
});
var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#dtable').DataTable({
        "ajax": {
            "url" : "/Admin/Order/GetAll"
        },
        "columns": [
            {"data" : "id" , "width":"5%"},
            {"data" : "name" , "width":"25%"},
            {"data" : "phoneNumber" , "width":"15%"},
            { "data": "user.email" , "width":"15%"},
            { "data": "orderStatus", "width": "15%" },
            { "data": "orderTotal", "width": "10%" },
            {
                "data": "id",
                "render": function (data) {
                    return `
                        <div class="w-75 btn-group" role="group">
                            <a class="btn btn-primary m-1" href="/Admin/Order/Details?orderId=${data}"><i class="bi bi-pencil-square"></i></a>
                            
    
                        </div>

                        `
                },

                "width": "5%"
            },

            ]
        });
}
function Delete(url) {
    Swal.fire({
        title: 'Are you sure?',
        text: "You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    } else {
                        toastr.error(data.message);
                    }
                }
            })
        }
    })
}
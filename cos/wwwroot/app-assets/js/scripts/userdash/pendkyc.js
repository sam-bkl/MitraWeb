$(document).ready(function () {


    
    GetkycDetails();
    //GetUsg();
    //GetMsg();
    //GetData();
    //GetBarData();
    //GetLastupd();
    //GetLastupdSim();

});




function GetkycDetails() {

    // Destroy DataTable if already initialized
    if ($.fn.DataTable.isDataTable('#kycTable')) {
        $('#kycTable').DataTable().clear().destroy();
    }

    var tableBody = $('#table-body-kycpend');
    tableBody.html('<tr style="text-align:center"><td colspan="6">Loading...</td></tr>');

    $.ajax({
        type: 'POST',
        url: "/UserDash/GetkycDetails",
        headers: {
            "Content-type": "application/x-www-form-urlencoded; charset=UTF-8",
            'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
        },
        success: function (data) {

            var tableString = '';
            for (var i = 0; i < data.length; i++) {
                tableString += '<tr>';
                tableString += '<td>' + data[i].circle + '</td>';
                tableString += '<td>' + data[i].name + '</td>';
                tableString += '<td>' + data[i].adhaar + '</td>';
                tableString += '<td>' + data[i].kycflag + '</td>';
                tableString += '<td>' + data[i].entrydate + '</td>';
                // Add Approve button
                tableString += '<td>';
                tableString += '<button class="btn btn-success btn-sm approve-btn" onclick="approveKyc(' + data[i].id + ')">';
                tableString += '<i class="fa fa-check"></i> Approve</button>';
                tableString += '</td>';
                tableString += '</tr>';
            }

            tableBody.html(tableString);

            // Reinitialize the DataTable
            $('#kycTable').DataTable({
                "ordering": true,
                "paging": true,
                "searching": true
            });

        }
    });
}

function GetLastupd() {


    var data = {

    }

    $.ajax({
        type: 'POST',
        url: "/UserDash/GetLastupd",

        headers: {
            "Content-type": "application/x-www-form-urlencoded; charset=UTF-8",
            'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
        },
        data: data,
        success: function (data) {

            console.log(data);

            $('#last-data-upd').html(data.lastcdr);


        },
        failure: function (data) {
            console.log(data);
        }

    });

}

function GetLastupdSim() {


    var data = {

    }

    $.ajax({
        type: 'POST',
        url: "/UserDash/GetLastupdSim",

        headers: {
            "Content-type": "application/x-www-form-urlencoded; charset=UTF-8",
            'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
        },
        data: data,
        success: function (data) {

            console.log(data);


            $('#last-upd').html(data.lastSim);

        },
        failure: function (data) {
            console.log(data);
        }

    });

}

function GetUsg() {


    var data = {

    }

    $.ajax({
        type: 'POST',
        url: "/UserDash/GetUsage",

        headers: {
            "Content-type": "application/x-www-form-urlencoded; charset=UTF-8",
            'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
        },
        data: data,
        success: function (data) {
            //  console.log("hi");
            // console.log(data);
            //  console.log(data.total_data)

            $('#info-box-data').html((data.total_data / 1000).toFixed(2) + " GB");
            $('#info-box-sms').html(data.total_sms);

        },
        failure: function (data) {
            console.log(data);
        }

    });


}

function GetMsg() {


    var data = {

    }

    $.ajax({
        type: 'POST',
        url: "/UserDash/GetNotifications",

        headers: {
            "Content-type": "application/x-www-form-urlencoded; charset=UTF-8",
            'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
        },
        data: data,
        success: function (data) {
            // console.log("hi");
            //  console.log(data);
            //  console.log(data.total_data)

            $('#info-box-msg').html(data.total_msg);
            $('#info-box-msgin').html(data.total_msg);

        },
        failure: function (data) {
            console.log(data);
        }

    });


}

function GetMsgDetails() {
    var tableBody = $('#table-body-msgs');
    tableBody.html('<tr style="text-align:center"><td colspan="2">No data found</td></tr>');

    var data = {

    }

    $.ajax({
        type: 'POST',
        url: "/UserDash/GetNotificationsMsgs",

        headers: {
            "Content-type": "application/x-www-form-urlencoded; charset=UTF-8",
            'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
        },
        data: data,
        success: function (data) {
            // console.log("hi");
            //  console.log(data);
            //  console.log(data.total_data)

            console.log(data);

            // $('#info-mymsg').html(data.message); 
            var tableString = '';
            for (var i = 0; i < data.length; i++) {
                tableString += '<tr><td>' + data[i].message + '</td>';
                tableString += '<td>' + data[i].date + '</td>';
                tableString += '</tr>';

            }
            tableBody.html(tableString);
            showLoading();

        },
        failure: function (data) {
            console.log(data);
        }

    });


}

function GetData() {


    var data = {

    }

    $.ajax({
        type: 'POST',
        url: "/UserDash/GetTopSims",

        headers: {
            "Content-type": "application/x-www-form-urlencoded; charset=UTF-8",
            'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
        },
        data: data,
        success: function (data) {
            // console.log("hi");
            console.log(data);
            //  console.log(data.total_data)
            var labelSims = new Array();
            var usrdata = new Array();
            for (var i = 0; i < data.length; i++) {
                labelSims.push(data[i].msisdn);
                usrdata.push((data[i].usrdata).toFixed(2));
            }
            console.log(labelSims);
            console.log(usrdata);

            DonutChartTopSims(labelSims, usrdata);

        },
        failure: function (data) {
            console.log(data);
        }

    });


}

function GetBarData() {


    var data = {

    }

    $.ajax({
        type: 'POST',
        url: "/UserDash/GetBarUsg",

        headers: {
            "Content-type": "application/x-www-form-urlencoded; charset=UTF-8",
            'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
        },
        data: data,
        success: function (data) {
            // console.log("hi");
            console.log(data);
            //  console.log(data.total_data)
            var labelUsg = new Array();
            var mondata = new Array();
            var monsms = new Array();

            for (var i = 0; i < data.length; i++) {
                labelUsg.push(data[i].labl);
                mondata.push((data[i].custdata / 1000).toFixed(2));
                monsms.push(data[i].custsms);
            }
            console.log(labelUsg);
            console.log(mondata);
            console.log(monsms);

            //BarChartUsage(labelUsg,mondata,monsms);
            BarChartUsage(labelUsg, mondata);

        },
        failure: function (data) {
            console.log(data);
        }

    });


}


var donutChartTopSims = null;
function DonutChartTopSims(labelSims, usrdata) {

    if (donutChartTopSims != null) {
        donutChartTopSims.destroy();
    }

    var data = {
        labels: labelSims,

        datasets: [
            {
                data: usrdata,
                //  data: [700,500,400,600,300,100],
                backgroundColor: ['#e6194B', '#f58231', '#ffe119', '#bfef45', '#3cb44b', '#42d4f4', '#4363d8', '#911eb4', '#000000', '#808000'],
                //backgroundColor: ['#f56954', '#00a65a', '#f39c12', '#00c0ef', '#3c8dbc', '#d2d6de'],
            }
        ]
    }

    //-------------
    //- PIE CHART -
    //-------------
    // Get context with jQuery - using jQuery's .get() method.
    var donutChartCanvas = $('#donut-chart-top-sims').get(0).getContext('2d')
    var donutData = data;
    var donutOptions = {
        maintainAspectRatio: false,
        responsive: true,
    }
    //Create pie or douhnut chart
    // You can switch between pie and douhnut using the method below.
    donutChartTopSims = new Chart(donutChartCanvas, {
        type: 'doughnut',
        data: donutData,
        options: donutOptions
    });

}

var barChartUsage = null;
function BarChartUsage(labelUsg, mondata, monsms) {
    //function BarChartUsage(labelUsg,mondata) {

    if (barChartUsage != null) {
        barChartUsage.destroy();
    }

    var barChartData = {
        labels: labelUsg,
        //  labels  : ['January', 'February', 'March', 'April', 'May', 'June', 'July'],
        datasets: [
            {
                label: 'Data',
                backgroundColor: 'rgba(60,141,188,0.9)',
                borderColor: 'rgba(60,141,188,0.8)',
                pointRadius: false,
                pointColor: '#3b8bba',
                pointStrokeColor: 'rgba(60,141,188,1)',
                pointHighlightFill: '#fff',
                pointHighlightStroke: 'rgba(60,141,188,1)',
                data: mondata
                //  data                : [28, 48, 40, 19, 86, 27, 90]
            },
            {
                label: '',
                backgroundColor: 'rgb(255, 0, 0,0)',
                // borderColor: 'rgb(255, 0, 0,1)',
                // pointRadius: false,
                // pointColor: 'rgb(255, 0, 0,1)',
                // pointStrokeColor: '#c1c7d1',
                // pointHighlightFill: '#fff',
                // pointHighlightStroke: 'rgb(255, 0, 0,1)',
                // data: monsms
                //data                : [65, 59, 80, 81, 56, 55, 40]
            },
        ]
    }

    var barChartCanvas = $('#bar-chart-usage').get(0).getContext('2d')
    var barChartData = jQuery.extend(true, {}, barChartData)
    var temp0 = barChartData.datasets[0]
    var temp1 = barChartData.datasets[1]
    barChartData.datasets[0] = temp1
    barChartData.datasets[1] = temp0

    var barChartOptions = {
        responsive: true,
        maintainAspectRatio: false,
        datasetFill: false
    }

    barChartUsage = new Chart(barChartCanvas, {
        type: 'bar',
        data: barChartData,
        options: barChartOptions
    });


}

function showLoading() {
    $('#modal-status').modal('show');
}

function hideLoading() {
    $('#modal-status').modal('hide');
}

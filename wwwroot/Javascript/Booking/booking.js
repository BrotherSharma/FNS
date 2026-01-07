

    function checkDoubleBooking(seatId, empId, selectedDate, email) {
        var data1 = {
            "url": "http://localhost:5025/api/BookingMaster/getDetail?seatId=" + seatId + "&&empId=" + empId + "&&date1=" + selectedDate,

            "method": "GET",
            "timeout": 0,
            "headers": {
                "Content-Type": "application/json"
            }
        };
        console.log("Email: " + email);
debugger;
    console.log("DAAAAAAAAAAATTTTTTTEEEEEEEEEEE::::::::"+data1);


            $.ajax(data1).done(function (response) {
                if (response == 0) {
                    openAlert("Seat for this date is already booked for this employee!");
                }
                else {
                    var data = {
                        seatId: seatId,
                        empId: empId,
                        date1: selectedDate,
                        autoMode: "False",
                    }
                    var book = {
                        "url": "http://localhost:5025/api/BookingMaster/booking",
                        "method": "post",
                        "timeout": 0,
                        "headers": {
                            "Content-Type": "application/json"
                        },
                        "data": JSON.stringify(data),
                    }

                    $.ajax(book).done(function (response) {
                        if (response = "Seat Booked Successfully") {
                            debugger;
                            functionSuccessAlert("Success!", "Successfully Booked");
                            var bookingDetail = {
                                "url": "http://localhost:5025/api/BookingDetail/getoneseat?date1=" + selectedDate + "&&empid=" + empId,
                                "method": "get",
                                "timeout": 0,
                                "headers": {
                                    "Content-Type": "application/json"
                                }
                            }
                            debugger;
                            console.log(bookingDetail);
                            $.ajax(bookingDetail).done(function (response) {
                                if (response) {
                                    console.log(response)
                                    for (var i in response) {
                                        alert("Your booking deatail:\n floor number:" + response[i].floorNumber + " officeName: " + response[i].officeName + " tablename: " + response[i].tableName + " seat name: " + response[i].seatName);
                                        debugger;
                                        bookingemail(email, response[i].floorNumber, response[i].officeName, response[i].tableName, response[i].seatName);

                                        setDate();
                                    }
                                }
                            });
                        }
                    });

                }

            });
    }
    function seatBook() {
        $("#seatBookWindow").data("kendoWindow").center().close();
        var seatId = $('#hiddenSeatId').val();
        console.log(seatId);
        var empId = $('#empNameList').val();
         var empNameList = $("#empNameList").data("kendoDropDownList");
    var selectedEmp = empNameList.dataItem(); // Get the selected item
    var email = selectedEmp ? selectedEmp.email : ''; // Retrieve the email from the selected item

        console.log(email);

        var date1 = $("#datepicker").val();
        var d = new Date(date1),
            month = '' + (d.getMonth() + 1),
            day = '' + d.getDate(),
            year = d.getFullYear();

        if (month.length < 2)
            month = '0' + month;
        if (day.length < 2)
            day = '0' + day;

        var selectedDate = [year, month, day].join('-');
        checkDoubleBooking(seatId, empId, selectedDate, email);
    }
    function dateNow() {
        var now = new Date();

        var day = ("0" + now.getDate()).slice(-2);
        var month = ("0" + (now.getMonth() + 1)).slice(-2);
        $("#datepicker").kendoDatePicker({
            value: new Date(now.getFullYear(), month, day),
            format: "dd-MMM-yyyy",
            parseFormats: ["dd-MMM-yyyy"],
            depth: "month",
            start: "month"

        });
        var datepicker = $("#datepicker").data("kendoDatePicker");
        datepicker.value(new Date());
        datepicker.min(new Date());
        var today = now.getFullYear() + "-" + month + "-" + day;
    }
    function setDate() {
        var date1 = $("#datepicker").val();
        var d = new Date(date1),
            month = '' + (d.getMonth() + 1),
            day = '' + d.getDate(),
            year = d.getFullYear();

        if (month.length < 2)
            month = '0' + month;
        if (day.length < 2)
            day = '0' + day;

        if ($("#globalFlag").val().localeCompare("") == 0) {

            $('#floor').val(0);
            $('#office').val(0);
            fillFrame();
        }
        else {

            fillFrame($("#office").val());
        }
    }




    function floor() {
        var data1 = {
            "url": "http://localhost:5025/api/FloorMasterApi",
            "method": "GET",
        }
        var fillData = [];

        fillData.push({ "floorId": "0", "floorNumber": "All Floor" })

        $.ajax(data1).done(function (response) {
            for (var i in response) {
                fillData.push({ "floorId": response[i].c_id, "floorNumber": response[i].floorNumber })
            }
            $("#floor").kendoDropDownList({
                dataTextField: "floorNumber",
                dataValueField: "floorId",
                dataSource: fillData,
                SelectedIndex: "0",
                change: function (e) {
                    var floorId = this.value();
                    //alert(floorId);
                    filleOffice(floorId);
                    fillFrame(floorId);

                }
            });

        });
    }
    function filleOffice(floorId) {

        office(floorId)
        document.getElementById("divOffice").style.display = "block";
    }
    function office(floorId) {
        var url1 = "http://localhost:5025/api/OfficeMaster/" + floorId;
        if (floorId == 0) {
            var url1 = "http://localhost:5025/api/OfficeMaster/getofficedetails/";
        }


        var data1 = {
            url: url1,
            "method": "GET",
            "timeout": 0,
            "headers": {
                "Content-Type": "application/json"
            },

        }
        var fillData = [];
        fillData.push({ "officeId": "0", "officeName": "All Office" })

        $.ajax(data1).done(function (response) {

            for (var i in response) {
                fillData.push({ "officeId": response[i].officeId, "officeName": response[i].officeName })
            }

            $("#office").kendoDropDownList({
                dataTextField: "officeName",
                dataValueField: "officeId",
                dataSource: fillData,

                SelectedIndex: "0",
                change: function (e) {
                    var officeId = this.value();
                    fillFrame(officeId);
                    $("#globalFlag").val("true");
                }
            });


        });
    }
    function fillFrame(officeId) {
    var url1 = "http://localhost:5025/api/TableMaster/api/TableMaster/GetByOfficeId/" + officeId;
    document.getElementById("caspointSeatWrapper").innerHTML = "";

    if ($('#floor').val() == 0 && $('#office').val() == 0) {
        url1 = "http://localhost:5025/api/TableMaster";
    }
    if ($('#floor').val() != 0 && $('#office').val() == 0) {
        url1 = "http://localhost:5025/api/TableMaster/GetByFloorId/" + $('#floor').val();
    }

    var settings = {
        "url": url1,
        "method": "GET",
        "timeout": 0,
        "async": false,
    };

    var date1 = $("#datepicker").val();
    var d = new Date(date1),
        month = '' + (d.getMonth() + 1),
        day = '' + d.getDate(),
        year = d.getFullYear();

    if (month.length < 2)
        month = '0' + month;
    if (day.length < 2)
        day = '0' + day;

    selectedDate = [year, month, day].join('-');
    date1 = [year, month, day].join('-');
    $.ajax(settings).done(function(response) {
        for (var i in response) {
            console.log(i, response[i].c_tablename);
            var allSeatData = {
                "url": "http://localhost:5025/api/TableMaster/GetSeatByTableId/" + response[i].c_id,
                "method": "GET",
                "timeout": 0,
                "async": false,
            };
            var iDiv = document.createElement('div');
            iDiv.id = "table" + response[i].c_id;
            iDiv.className = 'seat-block-wrapper bg-white';
            document.getElementById('caspointSeatWrapper').appendChild(iDiv);

            var iDiv0 = document.createElement('div');
            iDiv0.id = "t" + response[i].c_id;
            iDiv0.className = 'border b-radius';
            document.getElementById("table" + response[i].c_id).appendChild(iDiv0);

            document.getElementById("t" + response[i].c_id).innerHTML = '<div class="seat-block fs-20 text-dark"><span><b>' + response[i].c_tablename + '</b><br/><p style="font-size: 14px;">(' + response[i].c_description + ')</p></span></div>';
            var iDiv2 = document.createElement('div');
            iDiv2.id = "b" + response[i].c_id;
            iDiv2.className = 'seat-location';
            document.getElementById("t" + response[i].c_id).appendChild(iDiv2);

            var rptSeatId = ""; // Move this line outside the inner AJAX success function
            var cnt = 0;
            var selectedDate = "2024-04-10"; 
            // debugger;
            $.ajax(allSeatData).done(function(seatData) {
                // console.log(seatData);
                for (var j in seatData) {
                    var newDate = null;

                    if (seatData[j] && seatData[j].date1) {
                        var dateParts = seatData[j].date1.split('-');
                        if (dateParts.length === 3) {
                            newDate = dateParts[2] + '-' + dateParts[1] + '-' + dateParts[0];
                        }
                    }
                    
                    debugger;
                    console.log(seatData[j]);
                    console.log(date1);
                          if (seatData[j].isBooked == "True" && seatData[j].isDated == date1) {
                                str = '<button class="btn btn-block btn-raised btn-success" onmouseover="dynToolTip(\'' + seatData[j].fName + '\',\'' + seatData[j].lName + '\',\'' + seatData[j].teamName +'\',\'' + seatData[j].departmentName +'\',\'' + seatData[j].email +'\',\'' + seatData[j].loginId +'\',\'' + seatData[j].empCode +'\',\'' + seatData[j].doj + '\')">'+ seatData[j].seatName + "-" + seatData[j].fName +'</button>';
                        
                        var iDiv3 = document.createElement('div');
                        iDiv3.id = 'seat' + seatData[j].seatId;
                        iDiv3.className = '';
                        document.getElementById("b" + response[i].c_id).appendChild(iDiv3);
                        document.getElementById('seat' + seatData[j].seatId).innerHTML = str;
                        continue;
                    }
                    else if (seatData[j].isReserved == "True") {
                        str = '<button class="btn btn-block btn-raised btn-danger" onclick="openAlertRelease(\'' + seatData[j].seatId + '\');">' + seatData[j].seatName + '</button>';
                        var iDiv3 = document.createElement('div');
                        iDiv3.id = 'seat' + seatData[j].seatId;
                        iDiv3.className = '';
                        document.getElementById("b" + response[i].c_id).appendChild(iDiv3);
                        document.getElementById('seat' + seatData[j].seatId).innerHTML = str;
                        continue;
                    }

                    if (newDate == "--") {
                        var str = '<button class="btn btn-block btn-raised bg-light" onclick="openBookWindow(\'' + seatData[j].seatId + '\');">' + seatData[j].seatName + '</button>';

                        var iDiv3 = document.createElement('div');
                        iDiv3.id = 'seat' + seatData[j].seatId;
                        iDiv3.className = '';

                        document.getElementById("b" + response[i].c_id).appendChild(iDiv3);
                        document.getElementById('seat' + seatData[j].seatId).innerHTML = str;

                        document.getElementById('seat' + seatData[j].seatId).innerHTML = str;
                    } else {
                        if (rptSeatId != seatData[j].seatId) {
                            cnt = 0;
                            var iDiv3 = document.createElement('div');
                            iDiv3.id = 'seat' + seatData[j].seatId;
                            iDiv3.className = '';
                            var str = "";
                            document.getElementById("b" + response[i].c_id).appendChild(iDiv3);
                            if (newDate == selectedDate) {
                                console.log("Rendering seat for selected date:", seatData[j].seatId);
                                //@* str = '<button class="btn btn-block btn-raised btn-success" </button>'; *@

                            } else {
                                var str = '<button class="btn btn-block btn-raised bg-light" onclick="openBookWindow(\'' + seatData[j].seatId + '\');">' + seatData[j].seatName + '</button>';
                            }

                            document.getElementById('seat' + seatData[j].seatId).innerHTML = str;

                        }
                        if (rptSeatId == seatData[j].seatId) {
                            if (newDate == selectedDate) {
                                document.getElementById('seat' + seatData[j].seatId).remove();
                                var iDiv3 = document.createElement('div');
                                iDiv3.id = 'seat' + seatData[j].seatId;
                                iDiv3.className = '';
                                var str = "";
                                document.getElementById("b" + response[i].c_id).appendChild(iDiv3);

                                str = '<button class="btn btn-block btn-raised btn-success" ';
                                document.getElementById('seat' + seatData[j].seatId).innerHTML = str;
                            }
                        }
                    }

                    storedSeatId = seatData[j].seatId;
                }
            });

        }
    });
}

    var storedSeatId; // Declare a global variable

    function getBookedSeats(date, seatid) {
        $.ajax({
            url: 'http://localhost:5025/api/BookingDetail/SeatBook?date=' + date,
            type: 'GET',
            data: {
                date: date,
                seatid: seatid
            },
            success: function (data) {
                console.log('Response from server:', data);
                // Assuming 'data' is the response from the server
                // Store the 'seatid' in the global variable
                storedSeatId = data.seatid;

                // Loop through each seat ID and change the button color if it matches
                data.forEach(function (seat) {
                    var button = document.getElementById('button_' + seat.seatid);
                    if (button) {
                        // Change button color to green
                        button.style.backgroundColor = 'green';
                    }
                });
            },
            error: function (xhr, status, error) {
                // Handle error
                console.error(error);
            }
        });
    }

    // Function to fetch seat booking status and update seat color
    function updateSeatColors() {
        // Fetch seat IDs from the API
        $.ajax({
            url: "http://localhost:5025/api/BookingDetail/SeatBook?date=2024-04-30",
            method: "GET",
            success: function (response) {
                // Iterate through the response and update seat colors
                response.forEach(function (seat) {
                    // Change color of the seat with matching seat ID
                    var seatElement = document.getElementById('seat' + seat.seatId);
                    if (seatElement) {
                        seatElement.classList.remove('btn-danger'); // Remove red color
                        seatElement.classList.add('btn-success'); // Add green color
                    }
                });
            },
            error: function (xhr, status, error) {
                console.error("Error fetching booked seats:", error);
            }
        });
    }

    // Call the function to update seat colors

    function openBookWindow(seatId) {

        $('#hiddenSeatId').val(seatId);
   
            console.log(seatId);
        var settings = {
            "url": "http://localhost:5025/api/empLogin/getEmployeeList",
            "method": "GET",
            "timeout": 0,
        };
        var data = [];
        var src = "";
        $.ajax(settings).done(function (response) {
            for (var i in response) {
                src = "http://localhost:5025/api/UserApi?id=263";
                var flag = LoadImage1(response[i].loginId);
                if (LoadImage1(response[i].loginId) == 1) {
                    src = "";
                }

                //'src': src ,
                data.push({ 'title': 'Select', 'loginId': response[i].loginId, 'src': src, 'fName': response[i].fName, 'lName': response[i].lName, 'empId': response[i].empId, 'email': response[i].email });
            }

            $("#empNameList").kendoDropDownList({
                filterable: true,
                filter: "contains",
                size: "large",
                dataTextField: "email",
                dataValueField: "empId",
                //dataTextField: "email",
                //dataValueField: "empId",
                dataSource: data,
                change: function (e) {
                    //  debugger
                    //this.dataTextField = this.value();
                    var value = this.value();
                    //alert(value);

                },

                template: "<div style='display:-webkit-inline-box;  width:100px;'><img class='ImgDiv img-circle' id='dropImg' style='border:none' src='#: src#' /></div><div style='display:inline-block; '><span>#: email #</span><br /><br><span>#: fName ##: lName #</span></div>",
            });


        });

        $("#seatBookWindow").data("kendoWindow").center().open();
    }


    function openAlert(msg) {
        kendo.alert(msg);
    }
    function openAlertRelease(seatId) {
        //debugger

        //document.getElementById("lblHiddenSeatId") = seatId;
        $("#lblHiddenSeatId").val(seatId);

        $("#confirmReleaseSeat").data("kendoWindow").center().open();
    }
    function reserveSeat() {
        // alert("hi");
        $.ajax({
            url: 'http://localhost:5025/api/BookingMaster/Reserve?seatId=' + $("#hiddenSeatId").val() + '&&operation=block',
            type: 'POST',
            success: function (status) {
                if (status) {
                    setDate();
                }
            },
            error: function (error) {
                console.log(error);
            }
        });
        $("#seatBookWindow").data("kendoWindow").center().close();
    }
    function releaseSeat() {
        $.ajax({
            url: 'http://localhost:5025/api/BookingMaster/Reserve?seatId=' + $("#lblHiddenSeatId").val() + '&&operation=release',
            type: 'POST',
            success: function (status) {
                if (status) {
                    setDate();
                }
            },
            error: function (error) {
                console.log(error);
            }
        });
        $("#confirmReleaseSeat").data("kendoWindow").center().close();

    }
    function closeKendoWindow() {
        $("#seatBookWindow").data("kendoWindow").close();
    }

    function chackProfileExist(loginId) {

        $.ajax({

            type: 'GET',
            data: "",
            //data: fileUpload.files,
            cache: false,
            contentType: false,
            processData: false,
            success: function (status) {
                return status
            },
            error: function (error) {
                console.log(error);

            }
        });

    }
    $(document).ready(function () {
        $("#globalFlag").val("");
        dateNow();
        floor();
        office(0);
        fillFrame(0);
        var dataSource = new kendo.data.DataSource({
            transport: {
                read: {
                    url: "https://demos.telerik.com/kendo-ui/service/Products",
                    dataType: "jsonp"
                }
            },
        });

        $("#thumbnail_grid").kendoListView({
            dataSource: dataSource,
            selectable: "multiple",
            template: kendo.template($("#thumnail_template").html())
        });

        $("#seatBookWindow").kendoWindow({
            draggable: false,
            resizable: false,
            width: "500px",
            height: "200px",
            title: "Seat Booking",
            visible: false,
            actions: ["Close"],
            modal: true
        });

        $("#confirmReleaseSeat").kendoWindow({
            draggable: false,
            resizable: false,
            width: "500px",
            height: "150px",
            title: "",
            visible: false,
            actions: ["Close"],
            modal: true
        });
        var str = '<li tabindex="-1" role="option" unselectable="on" class="k-item" aria-selected="false" data-offset-index="1"><div style="display:-webkit-inline-box;  width:100px;">img</div><div style="display:inline-block; width:100px;"><span>email</span><br /><br><span>fname</span></div></li>';


        $(".seat-wrapper").kendoTooltip({

            autoHide: true,
            filter: ".btn-success",

            content: '',
            //kendo.template($("#template").html()),
            width: 480,

            callout: false,
            position: "bottom right",
            show: function (e) {
                //alert("123");
                console.log($(e.sender.popup.wrapper));
                $(e.sender.popup.wrapper).addClass("custom-popover").css("margin-top", "5px");
            }
        });



        $("#tabstrip").kendoTabStrip({
            animation: {
                open: {
                    effects: "fadeIn"
                }
            }
        });

        $("#borederTabstrip").kendoTabStrip({
            animation: {
                open: {
                    effects: "fadeIn"
                }
            }
        });


        setTimeout(function () { $("#load_screen").hide(); }, 10);

        $("select:not([multiple])").kendoDropDownList();

        $("#UserRoleDropdown").kendoDropDownList({
            popup: {
                appendTo: $(".userprofile-menu")
            }
        });
        //Kendo File Upload
        $("#fileProfileImage").kendoUpload({
            showFileList: false,
            multiple: false,
            localization: { select: 'Select File' }
        });

        $("#tabstrip").kendoTabStrip({
            animation: {
                open: {
                    effects: "fadeIn"
                }
            }
        });

        $("#borederTabstrip").kendoTabStrip({
            animation: {
                open: {
                    effects: "fadeIn"
                }
            }
        });

        var dataSource = new kendo.data.DataSource({
            transport: {
                read: {
                    url: "https://demos.telerik.com/kendo-ui/service/Products",
                    dataType: "jsonp"
                }
            },
        });

        $("#thumbnail_grid").kendoListView({
            dataSource: dataSource,
            selectable: "multiple",
            template: kendo.template($("#thumnail_template").html())
        });
    });



    function openAlert(msg) {
        kendo.alert(msg);
    }
    function openAlertRelease(seatId) {
        //debugger

        //document.getElementById("lblHiddenSeatId") = seatId;
        $("#lblHiddenSeatId").val(seatId);

        $("#confirmReleaseSeat").data("kendoWindow").center().open();
    }

    function LoadImage1(loginId) {
        var a = 0;
        $.ajax({
            type: 'GET',
            data: "",

            async: false,
            cache: false,
            contentType: false,
            processData: false,
            success: function (status) {
                if (status) {
                    a = 1;
                }
            },
            error: function (error) {
                console.log(error);
            }
        });

        return a;
    }
    function selected(e) {
        //debugger
        $("#empNameList").data("kendoDropDownList").dropdownlist({
            dataTextField: "email",
            dataValueField: "empId",
        });

    }
    function bookingemail(email, floorNumber, officeName, tableName, seatName) {
    debugger;
    var emailBody = "Your booking details:\n" +
                    "Floor number: " + floorNumber + "\n" +
                    "Office namezbxh: " + officeName + "\n" +
                    "Table name: " + tableName + "\n" +
                    "Seat name: " + seatName;

    var settings = {
        "url": "http://localhost:5025/api/BookingMaster/" + email,
        "data": { recipient: email, emailBody: emailBody },
        "method": "POST",
        "timeout": 0,
        "headers": {
            "Content-Type": "application/json"
        }
    };
    console.log(settings);
    $.ajax(settings).done(function (response) {
        setTimeout(function () {
            window.location = '/Dashboard/Booking';
        }, 3000);
        console.log("Done by Bhargav");
    });
}

    function myFunction() {

    }
    function dynToolTip(fName, lName, teamName, departmentName, email, loginId, empCode, doj) {
        //debugger
        document.getElementById("PEmpName").innerText = fName + " " + lName;
        document.getElementById("pTeamName").innerText = teamName;
        document.getElementById("pDepartmentName").innerText = departmentName;
        document.getElementById("pEmail").innerText = email;
        document.getElementById("pEmpCode").innerText = empCode;
        document.getElementById("PDate").innerText = doj.substring(0, 10);

        var loginId1 = "";
        var jsonData = {
            email: email,
        };
        $.ajax({
            async: false,
            method: "post",
            data: jsonData,
            success: function (data) {
                //alert(data);
                loginId1 = data;
            },
            error: function (error) {
                alert(error);
            }
        });

        //alert(loginId1);
        $("#hoverProfile").attr("src", "/Images/defaultProfile.jpg");
        if (LoadImage1(loginId1) == 1) {
            //alert("if");
            $("#hoverProfile").attr("src", "/Images/" + loginId1 + ".jpg");
        }
        $(".seat-wrapper").data("kendoTooltip").options.content = kendo.template($("#template").html());
    }

    function fname() {
    var fname = document.getElementById("fname").value;
    if (fname == "") {
        document.getElementById("fnameValidateMessage").style.color = "red";
        document.getElementById("fname").style.borderColor = "red";
        document.getElementById("fnameValidateMessage").innerHTML = "Please enter the first name.";
        return false;
    }
    else if (/^[A-Za-z]+$/.test(fname)) {
        document.getElementById("fnameValidateMessage").style.color = "lightgrey";
        document.getElementById("fname").style.borderColor = "lightgrey";
        document.getElementById("fnameValidateMessage").innerHTML = "";
        return true;
    }
    else {
        document.getElementById("fnameValidateMessage").style.color = "red";
        document.getElementById("fname").style.borderColor = "red";
        document.getElementById("fnameValidateMessage").innerHTML = "Please use the letters only";
        return false;
    }
}


function lname() {
    var lname = document.getElementById("lname").value;
    if (lname == "") {
        document.getElementById("lnameValidateMessage").style.color = "red";
        document.getElementById("lname").style.borderColor = "red";
        document.getElementById("lnameValidateMessage").innerHTML = "Please enter the last name.";
        return false;
    }
    else if (/^[A-Za-z]+$/.test(lname)) {
        document.getElementById("lnameValidateMessage").style.color = "lightgrey";
        document.getElementById("lname").style.borderColor = "lightgrey";
        document.getElementById("lnameValidateMessage").innerHTML = "";
        return true;
    }
    else {
        document.getElementById("lnameValidateMessage").style.color = "red";
        document.getElementById("lname").style.borderColor = "red";
        document.getElementById("lnameValidateMessage").innerHTML = "Please use the letters only.";
        return false;
    }
}
var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
function email() {
    var mail = document.getElementById("emailId").value;
    var name = mail.replace(/@.*$/, "");
    var check = /^[0-9]*$/.test(name);

    var emailExp = /^[a-zA-Z0-9][a-zA-Z0-9/.]+\@(([a-zA-Z0-9])+\.)+([a-zA-Z0-9]{2,4})+$/;
    if (mail == "") {
        document.getElementById("emailId").focus();
        document.getElementById("emailIdValidateMessage").style.color = "red";
        document.getElementById("emailId").style.borderColor = "red";
        document.getElementById("emailIdValidateMessage").innerHTML = "Please enter an email address.";
        //console.log("empty email");
        return false;
    }
    else if (emailExp.test(mail) && !check) {
        document.getElementById("emailIdValidateMessage").style.color = "lightgrey";
        document.getElementById("emailId").style.borderColor = "lightgrey";
        document.getElementById("emailIdValidateMessage").innerHTML = "";
        //console.log("regular wxp email");
        return true;
    }
    else {
        document.getElementById("emailId").focus();
        document.getElementById("emailIdValidateMessage").style.color = "red";
        document.getElementById("emailId").style.borderColor = "red";
        document.getElementById("emailIdValidateMessage").innerHTML = "Please enter the valid email address(Example: example@gmail.com).";
        //console.log("else part");
        return false;

    }

}
function pswd() {
    var password = document.getElementById("password").value;
    if (password == "") {
        document.getElementById("passwordValidateMessage").style.color = "red";
        document.getElementById("password").style.borderColor = "red";
        document.getElementById("passwordValidateMessage").innerHTML = "Please enter the password.";
        return false;
    }
    else if (/^(?=.*[0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*[*.!@$%^&]).{8,15}$/.test(password)) {
        document.getElementById("passwordValidateMessage").style.color = "lightgrey";
        document.getElementById("password").style.borderColor = "lightgrey";
        document.getElementById("passwordValidateMessage").innerHTML = "";
        return true;
    }
    else {
        document.getElementById("passwordValidateMessage").style.color = "red";
        document.getElementById("password").style.borderColor = "red";
        document.getElementById("passwordValidateMessage").innerHTML = "Please enter the valid password(Password should be of atleast 1 special characters, 1 number, 1 Capital letter and 1 small letter. Length must be 8 to 15 characters).";
        return false;
    }
}

function cpswd() {
    var cpassword = document.getElementById("confirmPassword").value;
    var password = document.getElementById("password").value;
    if (cpassword == "") {
        document.getElementById("confirmPasswordValidateMessage").style.color = "red";
        document.getElementById("confirmPassword").style.borderColor = "red";
        document.getElementById("confirmPasswordValidateMessage").innerHTML = "Please confirm the password";
        return false;
    }
    else if (password == cpassword) {
        document.getElementById("confirmPasswordValidateMessage").style.color = "lightgrey";
        document.getElementById("confirmPassword").style.borderColor = "lightgrey";
        document.getElementById("confirmPasswordValidateMessage").innerHTML = "";
        return true;
    }
    else {
        document.getElementById("confirmPassword").focus();
        document.getElementById("confirmPasswordValidateMessage").style.color = "red";
        document.getElementById("confirmPassword").style.borderColor = "red";
        document.getElementById("confirmPasswordValidateMessage").innerHTML = "Password does not match.";
        return false;
    }
}

function phone() {
    var phone = document.getElementById("phone").value;
    if (phone == "") {
        document.getElementById("phoneValidateMessage").style.color = "red";
        document.getElementById("phone").style.borderColor = "red";
        document.getElementById("phoneValidateMessage").innerHTML = "Please enter the phone number.";
        return false;
    }
    else if (/^[1-9]{1}[0-9]{9}$/.test(phone))
    {
        document.getElementById("phoneValidateMessage").style.color = "lightgrey";
        document.getElementById("phone").style.borderColor = "lightgrey";
        document.getElementById("phoneValidateMessage").innerHTML = "";
        return true;
    }
    else
    {
        document.getElementById("phoneValidateMessage").style.color = "red";
        document.getElementById("phone").style.borderColor = "red";
        document.getElementById("phoneValidateMessage").innerHTML = "Please enter the valid phone number.";
        return false;
    }
}

function checkCondition() {
    var check = document.getElementById("rememeberMe");
    if (check.checked) {
        document.getElementById("checkValidateMessage").innerHTML = "";
        return true;
    }
    else {
        document.getElementById("rememeberMe").focus();
        document.getElementById("checkValidateMessage").style.color = "red";
        //document.getElementById("rememberMe").style.border = "1px solid red";
        document.getElementById("checkValidateMessage").innerHTML = "<br>Please accept the end user license agreement.";
        return false;
    }
}

function validate() {

    var chkfname = fname();
    var chklname = lname();
    var chkemail = email();
    var chkpasword = pswd();
    var chkconfrimpassword = cpswd();
    var chkphone = phone();
    var chkCheck = checkCondition();
    // validation for check box

    if (chkfname && chklname && chkemail && chkpasword && chkconfrimpassword && chkphone && chkCheck)
    {
        return true;
            
    }
    else
    {
        return false;
    }
}

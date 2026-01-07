function email() {
    var mail = document.getElementById("c_email").value;

    // Simple email validation regex pattern
    var emailExp = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    if (mail == "") {
        // Handle empty email
        document.getElementById("c_email").focus();
        document.getElementById("emailIdValidateMessage").style.color = "red";
        document.getElementById("c_email").style.borderColor = "red";
        document.getElementById("emailIdValidateMessage").innerHTML = "Please enter an email address.";
        return false;
    }
    else if (emailExp.test(mail)) {
        // Valid email
        document.getElementById("emailIdValidateMessage").style.color = "lightgrey";
        document.getElementById("c_email").style.borderColor = "lightgrey";
        document.getElementById("emailIdValidateMessage").innerHTML = "";
        return true;
    }
    else {
        // Invalid email format
        document.getElementById("c_email").focus();
        document.getElementById("emailIdValidateMessage").style.color = "red";
        document.getElementById("c_email").style.borderColor = "red";
        document.getElementById("emailIdValidateMessage").innerHTML = "Please enter the valid email address(Example: example@gmail.com)";
        return false;
    }
}

function pswd() {
    var password = document.getElementById("c_password").value;
    if (password == "") {
        document.getElementById("passwordValidateMessage").style.color = "red";
        document.getElementById("c_password").style.borderColor = "red";
        document.getElementById("passwordValidateMessage").innerHTML = "Please enter the password.";
        return false;
    }
    else if (/^(?=.*[0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*[*.!@$%^&])/.test(password)) {
        document.getElementById("passwordValidateMessage").style.color = "lightgrey";
        document.getElementById("c_password").style.borderColor = "lightgrey";
        document.getElementById("passwordValidateMessage").innerHTML = "";
        return true;
    }
}

function checkCondition() {
    var check = document.getElementById("acceptTs");
    if (check.checked) {
        document.getElementById("checkValidateMessage").innerHTML = "";
        return true;
    }
    else {
        document.getElementById("checkValidateMessage").style.color = "red";
        document.getElementById("checkValidateMessage").innerHTML = "<br>Please accept the end user license agreement.";
        return false;
    }
}

function validateLoginForm() {
    var chkemail = email();
    var chkpassword = pswd();
    var chkCheck = checkCondition();

    if (chkCheck && chkemail && chkpassword) {
        return true;
    }
    else {
        return false;
    }
}

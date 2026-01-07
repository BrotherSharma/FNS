document.getElementById("loginBtn").addEventListener("click", function(event) {
    event.preventDefault(); // Prevent form submission to check validation

    let isValid = true;

    // Clear previous errors
    const fields = document.querySelectorAll(".k-textbox.login-textbox");
    fields.forEach(field => field.classList.remove("error"));
    document.querySelectorAll(".error-message").forEach(msg => msg.textContent = "");

    // Validate Email
    const email = document.getElementById("c_email");
    const emailPattern = /^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,6}$/;
    if (!emailPattern.test(email.value.trim())) {
        email.classList.add("error");
        document.getElementById("emailIdValidateMessage").textContent = "Please enter a valid email.";
        isValid = false;
    }

    // Validate Password
    const password = document.getElementById("c_password");
    if (password.value.trim().length < 6) {
        password.classList.add("error");
        document.getElementById("passwordValidateMessage").textContent = "Password must be at least 6 characters long.";
        isValid = false;
    }

    // If valid, submit the form (in this case, simulate a submit or handle form submission)
    if (isValid) {
        alert("Logged in successfully!");
        // Perform actual login logic here
    }
});

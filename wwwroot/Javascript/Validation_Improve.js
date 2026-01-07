// Improve.js

// Kendo UI Initialization

    $("#lifestyle").kendoDropDownList();
    $("#blood-type").kendoDropDownList();

    // Step Navigation
    const nextStepButton = $('.next-step');
    const step1 = $('.step-1');
    const step2 = $('.step-2');
    const submitButton = $('.submit-form');
    const submitSection = $('.submit-section');

    // Go to Step 2
    nextStepButton.on('click', function () {
        step1.hide();
        step2.show();
    });

    // Submit form (or handle it with an AJAX request)
    submitButton.on('click', function (e) {
        e.preventDefault(); // Prevent form submission for now

        // For demonstration, show a success message
        step2.hide();
        submitSection.show();

        // Optionally, submit the form data via AJAX or further handling
        // Here you can add the code to send the data to the server
    });


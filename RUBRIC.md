# COSC 335 Project 3 - Hardware Management: Assignment Rubric

*Copied from the Canvas assignment page for reference.*

## Purpose/Overview

In this assignment, your team will use your assigned programming language and operating system to build a simple application that connects to a webcam, displays live video, captures a still image, and applies an image filter. Your goal is to understand how application code reaches a physical device through libraries, operating-system services, and drivers, and to explain the work your team did to make the application run on every member's computer.

By the end of this assignment, you should be able to:

- Research and select appropriate APIs or libraries for accessing a webcam.
- Explain the roles of the application, libraries, operating-system services, device driver, and webcam in your solution.
- Display live camera frames, capture a still image, and apply a visible image transformation.
- Handle device access, permissions, and resource cleanup appropriately.
- Install, test, and troubleshoot the application on every team member's computer.
- Explain your code, research process, design decisions, and limitations.

## Estimated Time

Plan to spend approximately 6 hours completing this team project, including research, implementation, installation and testing on each computer, documentation, and demonstration preparation.

- Investigate the available camera APIs and libraries for your assigned environment.
- Implement webcam access, live display, still capture, and filtering.
- Install and test the application on every team member's computer.
- Record setup differences, problems, and solutions as you work.
- Prepare a concise 30-minute team demonstration and code walkthrough.
- Complete the research and contribution statements and final submission package.

## Team Requirements

Work with your team to complete this assignment. Every team member must participate in researching, implementing, and testing the solution and must understand the complete application.

The application must work on each team member's individual computer. One person finding a solution and copying it to everyone else's computer does not satisfy the collaboration requirement. Help one another install, test, diagnose problems, and explain what changed and why.

Any team member may be asked to explain:

- Which sources the team consulted and how they helped.
- Why the team selected its APIs or libraries.
- How the code connects to the camera and processes images.
- What problems occurred on their computer and how the team resolved them.
- How AI tools were used, if applicable, and what the team learned.
- What each important part of the code does and why it is necessary.

## Assigned Operating System and Programming Language

Use the same assigned operating system and programming language used for Projects 1 and 2 unless the instructor gives explicit permission to change.

Each member's demonstration must use the assigned environment on their own computer. If you use a virtual machine or another development environment, explain how it obtains access to the webcam and document any additional setup required.

Record the operating-system version, language/runtime version, relevant library versions, and camera used on each computer. Explain any differences that affected installation or behavior.

## Instructions

Build one application that completes all four parts below. A simple interface is sufficient as long as the live feed, captured image, and filtered results are clearly identifiable.

### 1. Research the Camera Interface and Device Stack

Find appropriate APIs or libraries that allow your assigned language to access the webcam in your assigned operating system. Explain why your team chose its approach and identify the documentation or examples that helped.

Prepare a simple diagram or clearly labeled explanation tracing the path from your application to the webcam. Identify, as applicable to your implementation:

- Your application and the camera calls it makes.
- The language library or framework used by the application.
- The underlying operating-system camera or media service/API.
- The device driver and its role in communicating with the webcam.
- The physical webcam and its connection to the computer.

Explain which details are established by documentation and which your team directly observed. If a layer is hidden by a library, identify that abstraction and explain what you could determine about it. You are not required to write a device driver.

### 2. Connect to the Webcam and Display Live Video

Your application must:

- Open a connection to an available webcam.
- Display a continuously updating live image in an application window or interface.
- Allow the user to stop the demonstration and close the application normally.
- Release the camera and other associated resources when finished.

Explain where the camera is opened, how frames reach your application, how the display updates, and how capture stops. Identify any permissions or device-selection settings needed in your environment.

Handle a failed camera connection or failed frame acquisition with a useful message or controlled exit. Explain how you tested the failure behavior; you do not need to reproduce every possible device failure.

### 3. Capture and Display a Still Image

Add a user action, such as a button or key press, that captures a single frame from the webcam and displays it as a still image in the application.

Make the distinction between the live feed and captured image clear. The captured image must remain available for inspection even as the live scene changes. Explain whether your code copies a frame or retains a reference to existing image data and why that choice works in your implementation.

Saving the image to a file is optional. Displaying only a prerecorded video or an existing image does not satisfy the webcam capture requirement.

### 4. Apply a Filter to Live Video and the Captured Image

Apply at least one visible image filter or transformation to both the live video and the captured still image. You may use the same filter for both.

Examples include grayscale, color inversion, thresholding, blur, or edge detection. Select an approach that your team can explain clearly.

Your application must make it possible to compare the original and filtered results, for example through a toggle or side-by-side views.

Explain:

- What the filter changes in the image data.
- Which function or code block performs the transformation.
- Whether the transformation occurs in your application, in a library, or through a camera setting.
- How the application keeps the original and filtered results available for comparison.
- Any visible effect on responsiveness or image quality and any limitations of your implementation.

## Code Requirements

Your code must include:

- Webcam connection and live image display.
- User-triggered still-image capture and display.
- Filtering of both live video and the captured image.
- A clear way to compare original and filtered results.
- Useful handling of camera connection or frame acquisition failures.
- A normal exit path that releases camera resources.
- Compatibility with your assigned operating system and programming language.
- Clear organization using functions, methods, or classes where appropriate.

Use meaningful names and comments that explain important device-access and image-processing decisions. Identify the purpose of imported libraries and any adapted example code. A small, understandable application is sufficient; an elaborate interface is not required.

## Required Demonstration

Each team will have 30 minutes to demonstrate and explain its application. Plan the demonstration so the instructor can see the required behavior on every team member's computer, hear the team's research and troubleshooting explanation, and ask questions.

1. **Working Application on Each Computer** - On each member's computer, show the live webcam feed, live filtering, still-image capture, the filtered still image, and a normal exit. Identify any setup or behavior differences between computers.
2. **Device Stack and Library Selection** - Trace the application-to-camera path using your diagram or labeled explanation. Explain the purpose of the APIs, libraries, operating-system services, and driver involved in your solution.
3. **Code Walkthrough** - Explain the important blocks for opening the camera, obtaining frames, updating the display, capturing a still image, applying the filter, handling errors, and releasing resources. Explain what each block does and why it is necessary, including the purpose of important parameters.
4. **Team Research and Problem Solving** - Describe how the team found and developed its solution. Identify the websites and documentation used, what the team adapted, and how you verified that the result worked. If you used AI, explain how it helped, what you checked or corrected, and what you learned. Discuss the actual problems encountered while setting up and running the application on each computer. Explain the symptoms, how you investigated them, what solved them, and how you verified the solution. If a computer required no special fixes, report that accurately.
5. **Collaboration, Tradeoffs, and Limitations** - Explain how every member participated in research, implementation, and testing. Discuss the strengths and limitations of your approach and one improvement you would make with more time. Every member must be prepared to explain the full application rather than only their own contribution.

## Required Submission

Submit the following through the Canvas assignment's file upload:

- Source code files and any additional assets needed to run the application.
- Setup and run instructions for your assigned environment, including dependencies and versions, camera permissions, device selection if applicable, and the controls for live filtering, still capture, and exit.
- A brief test record for each team member's computer. Identify the environment and camera, confirm each required feature, and describe any remaining limitations. Include evidence such as labeled screenshots of the live display and original/filtered captured image from each computer. Screenshots support the test record; the working application must also be demonstrated.
- The device-stack diagram or labeled explanation, with references supporting your implementation-specific claims.
- A brief research and troubleshooting record listing sources consulted, significant setup problems and solutions, and any AI assistance and how it was checked. State if no AI tools were used.
- A team contribution statement describing each member's role in research, implementation, and testing and how the team ensured that everyone understood the full solution.

## Instructor Check

Each team member may be asked questions about any part of the project. Possible questions include:

- Which call opens the camera, and what does its result tell you?
- What does this library provide, and which operating-system interface does it use?
- What is the device driver's role in your application?
- How does the program get and display the next frame?
- What keeps the captured image from changing with the live feed?
- What does your filter do to the image data?
- What happens if the camera cannot be opened or a frame cannot be read?
- Where are camera resources released, and why does that matter?
- What problem occurred on your computer, and how did you help solve it?
- Which source or AI suggestion did your team adapt, test, or correct?

All team members are responsible for understanding the complete project.

## Resources

- Official documentation for the camera and image-processing APIs or libraries selected by your team.
- Your assigned operating system's documentation for camera access, permissions, media services, and device support.
- Your programming language's documentation for dependency installation, error handling, and resource cleanup.
- Your development environment's documentation for running and debugging applications that access a camera.
- Your course notes and textbook sections on input/output, device drivers, and operating-system services.

### Tips

- Confirm basic camera access before adding capture controls and filters.
- Begin testing on all team members' computers early enough to investigate setup differences together.
- Record dependency versions and working configuration settings as you go.
- When troubleshooting, distinguish installation problems, camera permissions, device selection, frame acquisition, and display or image-format issues.
- Use clear labels such as LIVE, CAPTURED, ORIGINAL, and FILTERED.
- Make sure every member can launch, operate, explain, and close the application independently before the demonstration.

## Academic Integrity

You may consult documentation, course materials, approved learning resources, and online examples, but your team is responsible for understanding and explaining all submitted code. Identify sources and adapted code in your research record. If you use AI tools, explain how they helped, what you learned, and how your team reviewed, adapted, and tested the suggestions.

Do not submit code that your team cannot explain. Having one person supply the solution while other members only copy and run it does not satisfy this assignment's collaboration requirement and will substantially reduce the evaluation of team participation and understanding.

## Grading/Rubric

The assignment will be graded on working software, depth of understanding, and the team's collaborative problem solving. A running application alone is insufficient if team members cannot explain how they found, implemented, and tested the solution.

| Strong Submission | Weak Submission |
| --- | --- |
| Demonstrates the required application on every team member's computer. | Works only on one computer or lacks evidence of testing on the other computers. |
| Displays live video, captures a persistent still image, and applies a visible filter to both. | Omits a required feature or substitutes an existing image or prerecorded video for webcam capture. |
| Makes original and filtered results easy to compare and explains the transformation. | Shows a visual effect without explaining the code or how image data changes. |
| Accurately explains the application-to-device path and the roles of libraries, OS services, and drivers. | Names libraries without explaining how the application reaches the device. |
| Explains important code blocks, error handling, and resource cleanup. | Relies on copied code or cannot explain camera access, frame processing, or cleanup. |
| Documents research, AI use if applicable, and actual troubleshooting across computers. | Provides vague accounts of finding a solution or cannot explain problems and fixes. |
| Shows that every member helped research, implement, test, and understand the solution. | Relies on one person to find and distribute the solution while others only run it. |
| Provides reproducible setup instructions, test evidence, and an honest discussion of limitations. | Omits required setup information or makes claims unsupported by the demonstration and test record. |

*This submission counts for everyone in the COSC 335 OS Teams group.*

#include <WiFi.h>
#include <HTTPClient.h>
#include <ESP32Servo.h>


// Define the pin numbers for the control signals
int angleStepPin = 14;  // Pin connected to the step signal of TB6600
int angleDirPin = 12;   // Pin connected to the direction signal of TB6600
int enablePin = 13;     // Optional: Pin connected to the enable signal of TB6600 (set to -1 if not used)

int radialStepPin = 26;  // Pin connected to the step signal of TB6600
int radialDirPin = 27;   // Pin connected to the direction signal of TB6600

int penServoPin = 15;

int delayTime = 40;      // Delay time in microseconds
int angleStepSize = 1;   // Step size for angle movement
int radialStepSize = 1;  // Step size for radial movement

Servo penServo;
int penWritingPosition = 118;
int penNotWritingPosition = 108;
int penInitialPosition = penNotWritingPosition;




const char* ssid = "Kvasho";
const char* password = "11332244";

String preMode = "";



// URL for the POST request
const char* serverUrl = "https://kvash.tar.ge/GCode/fetchLatest";

const char* drawUrl = "https://kvash.tar.ge/GCode/drawing";
const char* setStatusFalse = "https://kvash.tar.ge/GCode/setDrawing?drawing=false";

int currM = 0;
int currR = 0;
int currA = 0;

// Function to initialize the stepper motor pins
void setupStepper(int stepPin, int dirPin, int enablePin = -1) {

    // Set pin modes
    pinMode(stepPin, OUTPUT);
    pinMode(dirPin, OUTPUT);
    if (enablePin != -1) {
        pinMode(enablePin, OUTPUT);
        digitalWrite(enablePin, LOW);  // Enable the motor driver (if needed)
    }
}

// Function to move the stepper based on instruction
void moveStepperByInstruction(int angle, int radial) {
    moveAngleStepper(angle);
    moveRadialStepper(radial);
}

// Function to move the angle stepper
void moveAngleStepper(int mode) {
    bool direction = (mode > 0);
    moveStepper(abs(angleStepSize * mode), direction, delayTime, angleDirPin, angleStepPin);
}

// Function to move the radial stepper
void moveRadialStepper(int mode) {
    bool direction = (mode > 0);
    moveStepper(abs(radialStepSize * mode), direction, delayTime, radialDirPin, radialStepPin);
}

// Function to move the stepper motor
// steps: number of steps to move
// direction: true for one direction, false for the opposite direction
// delayTime: time delay between steps in microseconds
void moveStepper(int steps, bool direction, int delayTime, int dirPin, int stepPin) {
    // Set the motor direction
    digitalWrite(dirPin, direction);

    // Pulse the step pin the required number of times
    for (int i = 0; i < steps; i++) {
        digitalWrite(stepPin, HIGH);
        delayMicroseconds(delayTime);  // Adjust delay to control speed
        digitalWrite(stepPin, LOW);
        delayMicroseconds(delayTime);
    }
}

// Function to convert radial to operations
void radialToOperationsConverter(int R, int A) {
    int pr = 0;  // Previous radial value
    int pa = 0;  // Previous angle value

    int mx = max(abs(R), abs(A));  // Max of absolute values
    int mn = min(abs(R), abs(A));  // Min of absolute values

    int r_sign = (R > 0) ? 1 : -1;  // Sign of R
    int a_sign = (A > 0) ? 1 : -1;  // Sign of A

    // Iterate over the range of the maximum value
    for (int i = 1; i <= mx; i++) {
        int more = i;
        int less = round((float)i * mn / mx);  // Calculate the scaled down value

        int r, a;
        if (abs(R) > abs(A)) {
            r = more;
            a = less;
        } else {
            r = less;
            a = more;
        }

        // Apply the signs to the values
        r *= r_sign;
        a *= a_sign;

        // Output the difference between current and previous values
        // Serial.print(r - pr);
        // Serial.print(" ");
        // Serial.println(a - pa);

        moveStepperByInstruction(a - pa, r - pr);

        pr = r;
        pa = a;
    }
}

void sendPostRequest() {
    // Check if Wi-Fi is connected
    if (WiFi.status() == WL_CONNECTED) {
        HTTPClient http;  // Create an HTTP client

        http.begin(serverUrl);                               // Specify the URL
        http.addHeader("Content-Type", "application/json");  // Set content type to JSON

        // Define the payload to send
        String jsonPayload = "{\"key1\":\"value1\",\"key2\":\"value2\"}";

        // Send the POST request and get the response code
        int httpResponseCode = http.POST(jsonPayload);

        String response = "";
        // Check the response code
        if (httpResponseCode > 0) {
            response = http.getString();  // Get the response payload
            Serial.println("Response code: " + String(httpResponseCode));
            // Serial.println("vaii");
            // Serial.println("response: " + response);
            // Serial.println("Response: " + response);
        } else {
            Serial.println("Error on sending POST: " + String(httpResponseCode));
        }

        http.end();  // Close the connection

        splitAndPrintLines(response);


    } else {
        Serial.println("WiFi not connected");
    }
}

bool shouldDraw() {
    // Check if Wi-Fi is connected
    if (WiFi.status() == WL_CONNECTED) {
        HTTPClient http;  // Create an HTTP client

        http.begin(drawUrl);                                 // Specify the URL
        http.addHeader("Content-Type", "application/json");  // Set content type to JSON

        String jsonPayload = "{\"key1\":\"value1\",\"key2\":\"value2\"}";

        // Send the POST request and get the response code
        int httpResponseCode = http.POST(jsonPayload);

        String response = "";
        // Check the response code
        if (httpResponseCode > 0) {
            response = http.getString();  // Get the response payload
            Serial.println("Response: " + response);
            // Serial.println("Response: " + response);
        } else {
            Serial.println("Error on sending POST: " + String(httpResponseCode));
        }

        if (response == "true") {
            return true;
        }
        return false;


        http.end();  // Close the connection
    } else {
        Serial.println("WiFi not connected");
    }
}

void setDrawStatusFalse() {
    // Check if Wi-Fi is connected
    if (WiFi.status() == WL_CONNECTED) {
        HTTPClient http;  // Create an HTTP client

        http.begin(setStatusFalse);                          // Specify the URL
        http.addHeader("Content-Type", "application/json");  // Set content type to JSON

        String jsonPayload = "{\"key1\":\"value1\",\"key2\":\"value2\"}";

        // Send the POST request and get the response code
        int httpResponseCode = http.POST(jsonPayload);

        String response = "";
        // Check the response code
        if (httpResponseCode > 0) {
            response = http.getString();  // Get the response payload
        } else {
            Serial.println("Error on sending POST: " + String(httpResponseCode));
        }

        splitAndPrintLines(response);

        http.end();  // Close the connection
    } else {
        Serial.println("WiFi not connected");
    }
}

void splitAndPrintLines(String data) {
    int startIndex = 0;
    int endIndex = data.indexOf('\n');  // Find the index of the first newline character

    // Loop through the string until there are no more newline characters
    while (endIndex != -1) {
        // Extract substring from startIndex to endIndex
        String line = data.substring(startIndex, endIndex);
        splitByWhitespace(line);
        // Serial.print(line);
        // Update startIndex to the character after the newline
        startIndex = endIndex + 1;

        // Find the index of the next newline character
        endIndex = data.indexOf('\n', startIndex);
    }
    splitByWhitespace("R0 0 0");
}

void splitByWhitespace(String line) {
    int idx = 0;

    int startIndex = 0;
    int endIndex = line.indexOf(' ');  // Find the index of the first space character
    String m = line.substring(startIndex, endIndex);

    startIndex = endIndex + 1;
    endIndex = line.indexOf(' ', startIndex);
    int r = line.substring(startIndex, endIndex).toInt();

    startIndex = endIndex + 1;
    endIndex = line.length();
    int a = line.substring(startIndex, endIndex).toInt();

    int dr = r - currR;
    int da = a - currA;

    //  Serial.println("m: " + m + ", R: " + r + ", A: " + a + ", dr: " + dr + ", da: " + da);
    if (m == "R0") {
        penServo.write(penNotWritingPosition);
    } else {
        penServo.write(penWritingPosition);
    }

    if (m != preMode) {
        delay(300);
    }
    preMode = m;

    radialToOperationsConverter(dr, da);

    currR = r;
    currA = a;
}


void setup() {
    Serial.begin(9600);  // Initialize serial communication

    penServo.attach(penServoPin);
    penServo.write(penInitialPosition);
    delay(100);
    // penServo.write()
    // Initialize stepper motors with appropriate pins
    setupStepper(radialStepPin, radialDirPin, enablePin);
    setupStepper(angleStepPin, angleDirPin, enablePin);


    WiFi.begin(ssid, password);
    Serial.print("Connecting to WiFi");
    while (WiFi.status() != WL_CONNECTED) {
        delay(500);
        Serial.print(".");
    }
    Serial.println("\nConnected to WiFi");

    // Call the function to send the POST request
    // sendPostRequest();
}

void loop() {

    shouldDraw();
    if (shouldDraw() == true) {
        Serial.println("started drawing");
        sendPostRequest();
        Serial.println("end drawing ");
        setDrawStatusFalse();
    }
    delay(1000);
    // // Example usage: Move stepper motors by instructions
    // radialToOperationsConverter(0, 27800);
    // // radialToOperationsConverter(0, -27800);
    // radialToOperationsConverter(3000, 27800);
    // delay(1000);
    // radialToOperationsConverter(-3000, -27800);
    // delay(1000);
}

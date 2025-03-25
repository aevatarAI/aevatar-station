import sys

try:
    # Check if the correct number of arguments is provided
    if len(sys.argv) < 2:
        print("Error: Missing argument! Please provide 'succ' or 'fail' as a parameter.")
        sys.exit(1)

    # Get the first argument
    command = sys.argv[1].lower()  # Convert to lowercase to handle case insensitivity

    # Execute appropriate logic based on the parameter value
    if command == "succ":
        print("Execution succeeded!")
        sys.exit(0)  # Return exit code 0 to indicate success
    elif command == "fail":
        print("Execution failed!")
        sys.exit(1)  # Return exit code 1 to indicate failure
    else:
        print("Error: Invalid argument! Please use 'succ' or 'fail' as input.")
        sys.exit(1)

except Exception as e:
    # Handle unexpected errors
    print(f"An error occurred: {e}")
    sys.exit(1)  # Return exit code 1 to indicate failure
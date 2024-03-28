import os
import re

def replace_icon_data(guid, fileID, regex=None):
    # Get the current directory
    current_directory = os.getcwd()
    
    # Compile regex if provided
    if regex:
        regex_pattern = re.compile(regex)
    
    # Iterate through files in the directory
    for root, dirs, files in os.walk(current_directory):
        for file in files:
            if regex and not regex_pattern.match(file):
                continue  # Skip files that don't match regex pattern
            if file.endswith('.meta'):
                file_path = os.path.join(root, file)
                # Read the content of the .meta file
                with open(file_path, 'r') as f:
                    lines = f.readlines()
                # Replace icon field data with input data
                for i in range(len(lines)):
                    if 'icon:' in lines[i]:
                        lines[i+2] = f'    fileID: {fileID}\n'
                        lines[i+3] = f'    guid: {guid}\n'
                        break
                # Write the modified content back to the file
                with open(file_path, 'w') as f:
                    f.writelines(lines)

# Example usage
guid = input("Enter GUID: ")
fileID = input("Enter FileID: ")
regex = input("Enter regex (optional): ")

replace_icon_data(guid, fileID, regex)
print("Replacement completed.")

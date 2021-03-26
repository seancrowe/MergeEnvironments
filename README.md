# Motivation
When moving to CHILI publish Online SaaS, you may want to merge environments. This tool will make the job nice and easy.

#Limitations
Error handling is limited - so if things go wrong oops.

Does not merge anything but the Resources at the moment. You can just merge everything else using the Windows File Explorer.

If there is a file conflict, so two files with the same name that are not data XMLs, the file from the primary will override the secondary
- This does not effect data XMLs, data XMLs are handled properly and either merged or renamed

# Usage
Download the from the Release page and unzip the file.

Open up a command prompt (with proper permissions) and change directory to the newly download zip file.

Then run the following command:
```
MergeEnvironments.exe <path to primary enviroment to merge> <path to secondary environment to merge>
```

So for example if I want to merge "A Environment" with "Other Environment", I would run this command:
```
MergeEnvironments.exe "C:\chili_data\A Environment" "C:\chili_data\Other Environment"
```

This will create a new merged environment in the local path of the MergeEnvironments.exe. If I do not want that I can specify an output path using the parameter --out-dir.

For example I want to merge "A Environment" with "Other Environment" to "Merged Environment", I would run this command:
```
MergeEnvironments.exe "C:\chili_data\A Environment" "C:\chili_data\Other Environment" --out-dir "C:\chili_data\Merged Environment
```

At the current moment this will only merge the Resources folder. So if I wanted to do a complete merge, I would just copy and paste the other folders (any except Resources) from "A Environment" to "Merged Environment". I would then do the same for "Other Environment" and just lest Windows File Explorer deal with any conflicts by overwriting them.

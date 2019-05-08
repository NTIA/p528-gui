# NOTICE! #
**This software is depends upon a proposed update to ITU-R Recommendation P.528 which will be considered during the May 2019 ITU-R Study Group 3 meetings.  This revision to P.528 is not yet the in-force Recommendation.  The results generated should be considered preliminary until this notice in the repository readme is removed.**

---

# ITU-R Rec P.528 GUI #

This repo contains a Graphical User Interface (GUI) frontend to [ITU-R Recommendation P.528](https://www.itu.int/rec/R-REC-P.528/en).  It uses the [U.S. Reference Implementation](https://github.com/NTIA/p528) of the model to render curves relating distance to propagation loss.  It allows a user set the model input parameters and generate a curve relating distance with loss.  In additional, this program allows the user to identify the three modes of propagation (Line-of-Sight, Diffraction, and Troposcatter), as well as allowing the user to export the curve to a CSV data file.

## Inputs ##

 * Height of the low terminal, in meters. Must be greater than or equal to 1.5 meters.
 * Height of the high terminal, in meters.  Must be greater than or equal to 1.5 meters.
 * Frequency, in MHz.  The lowest frequency is 125 MHz and the heightest frequency is 15 500 MHz.
 * Time percentage, which must be from 1 to 99.

## Outputs ##

The below image shows the tool rendering a curve with a low terminal height of 1.5 meters and a high terminal height of 1000 meters, for signal at 125 MHz at a 50% time percentage.  This corresponds to one of the curves shown in Figure 1-4a in the P.528 Recommendation.  The data representing this curve can be exported to a CSV data file through selecting the corresponding option in the File menu.
 
![Screenshot of P.528 GUI Tool](P528-Fig1-4a.png "Screenshot of P.528 GUI Tool")

## Distribution ##

To aquire a pre-built executable of this tool, navigate to the [Releases](https://github.com/NTIA/p528-gui/releases) page and download the most recent release.  Once downloaded, unzip the `.zip` file and place all files in the same folder.  Double-click on the `.exe` file to launch the application.

## Configure and Build ##

The tool is built on the .NET Framework and Windows Presentation Foundation, and is thus limited to execution on Microsoft Windows.  It uses the [Live Charts](https://github.com/Live-Charts/Live-Charts) package for the rendering of the plot. The [U.S. Reference Implementation](https://github.com/NTIA/p528) of the P.528 model is used to generate propagation loss relative to distance.

## References ##

 * [ITU-R Recommendation P.528](https://www.itu.int/rec/R-REC-P.528/en)
 * [U.S. Reference Implementation of Recommendation P.528](https://github.com/NTIA/p528)
 
# Contact #

For questions, contact Billy Kozma, (303) 497-6082, wkozma@ntia.gov

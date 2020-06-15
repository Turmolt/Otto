## Otto

Otto can be used to instantly fill out all of your public fields!

Once the submodule is imported using the steps below it is easy to use Otto:

    Once you name your public fields correctly, right click on the mono behaviour and select an auto populate option.
    
    EXAMPLE:
    I have a file Barking Dog.mp4 so I add public VideoClip BarkingDog; to my script and it will successfully find and assign the video clip from your asset database when Otto is invoked.
    Note: A space is added before every capital letter (GoodDog => "Good Dog") and _ is replaced with a space when searching the scene/asset database.

## Importing

Clone this repo as a submodule:

    In an existing / new Unity project, navigate to the directory where you want Otto (probably like Assets/_Vendor)
    git submodule add --depth 1 https://github.com/Turmolt/Otto.git Otto
    This should create a folder named 'Otto' within the current working directory.
    You should now have an up-to-date Otto ready to use in your project. Note that this process leaves Otto in a 'detached HEAD' state.

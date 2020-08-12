# About
The project contains different CPU and GPU flocking implementations in the Unity game engine, using a single thread and multithread approach, as well as another using compute shaders. All of them are mainly based on Unity's ECS implementation (which was presented by Mike Acton in GDC) and is also included in the project. All of the art assets are from Unity's demo. See Mike's technical presentation at https://youtu.be/p65Yt20pw0g.

I started this project when Unity had already been advertising for about a year their new tech stack "DOTS". I wanted to see what data-oriented design along with the job system and the burst compiler was able to do in terms of performance, so I decided to compare their implementation against other paradigms.

This project requires a specific version of the engine in order to work. It is highly recommended to use Unity's 2019.1.14f1 version.

Go to https://unity3d.com/es/get-unity/download/archive to find older releases of Unity.

# Preview
![alt-text](./GithubImgs/TeaserGif.gif)

# My system
The tests I did to get to the results below were made on an i7 4790k and 980ti, in a build, and at a resolution of 2560x1440. I was also using an SSD and 16gb of RAM.

# Some results
![alt-text](./GithubImgs/ResultsSpeedup.png)

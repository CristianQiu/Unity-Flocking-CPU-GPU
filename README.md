# About
The project contains different CPU and GPU flocking implementations in Unity's game engine, using a single thread and multithread approach, as well as another using compute shaders. All of them are mainly based on Unity's ECS implementation (which was presented by Mike Acton in GDC) and is also included in the project. The original Unity repository that includes the boids implementation using ECS can be found at https://github.com/Unity-Technologies/EntityComponentSystemSamples. All of the art assets are from Unity's demo. See Mike Acton technical presentation at https://youtu.be/p65Yt20pw0g.

I started this project in 2019 when Unity had already been advertising for about a year their new tech stack: DOTS. I wanted to see what data-oriented design along with the job system and the burst compiler was able to do in terms of performance, so I decided to compare their implementation against other paradigms, replicating their algorithm as much as I could, to make objective comparisons.

# Preview
The following gif showcases over 500k boids being simulated in compute shaders.

![alt-text](./GithubImgs/TeaserGif.gif)

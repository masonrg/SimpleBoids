# Boid Behaviour Simulation
<i>Boids simulation to explore flocking behaviours as described by Craig Reynolds in his original work on the subject, as well as predation, and environmental collision avoidance behaviours.</i>

This project is made using Unity 2018.2.14f1 for the CSC 305: Introduction to Computer Graphics course at the University of Victoria, and investigates the boid flocking ai behaviours such as cohesion, alignment, and collision avoidance, as well as predatory behaviours such as group disruption and target isolation.

----
## Demonstration Video
[![Demonstration](https://img.youtube.com/vi/8SM9PCxETZw/0.jpg)](https://www.youtube.com/watch?v=8SM9PCxETZw "Boids Simulation")

----
## A brief description of the main features of this project

- Customizable and editor-friendly path generation using Catmull-Rom splines; used by the flock leader boids
- Arc-length interpolation support along splines to provide consistent travel speeds 
- Boid flock behaviour modelling that faithfully simulates the avoidance, alignment, and cohesive behaviours characteristic to flocking algorithms
- Additional boid behaviours to introduce controlled randomness, willingness to follow the leader, and urgency for evading nearby predators
- Predator behaviours that extend basic boid behaviours and produce characteristics associated with disrupting flocks and isolating flock members, as well as eagerness to attack an isolated target and the willingness to withdraw from a failed attempt
- Custom boid models with dynamic wing flapping animations that correspond to speed and direction
- Environmental collision avoidance support for boids that ensures that obstacles are avoided when encountered

----
## Some usage notes

The scene contains a LeaderPath object and a BoidController object

- The LeaderPath object contains on it a CatmullRomSpline component that provides the customization and settings options for plotting and displaying the path to be used by the leader boid during the simulation.
- The BoidController object contains the settings to define boid behaviour. Does not include settings for boid predators, as those are modified on the predator object itself to allow different predators to exhibit different behaviours.

Prefabs spawned by the BoidController are the: BoidLeader, BoidDrone, and BoidPredator

- The BoidLeader directs boids by following the spline in the scene and serving as a basis direction vector for the flock. Speed to travel the spline at is customizable on the BoidLeader
- The BoidDrone contains an option to enable debug visibility when the boid is selected during simulation. This is primarily to display known neighbours and collision opportunities. Drone behaviour parameters are set on the BoidController as they are shared between all flock members.
- The BoidPredator contains the settings for defining predator behaviour, rather than being centrally controlled via the BoidController. These settings include weights for various predator behaviours such as: group disruption, target isolation, attack eagerness, withdrawal willingness, and independence(tendency to avoid other predators).

----
## Resources

[Eric Nordeus](https://www.habrador.com/tutorials/interpolation/1-catmull-rom-splines/)
- Helpful guide on Catmull-Rom splines within the context of C# and Unity.
	
[Craig Reynolds](https://www.red3d.com/cwr/papers/1987/boids.html)
- Original work on boid behaviour: "Flocks, Herds, and Schools:
A Distributed Behavioral Model"

// • How would you scale these systems for larger projects?
// It depends on many factors, for example in task3 i used simplified, but real production solutions
// For task5 for example, we could manage entities by StateMachine pattern with more complex logic of states

// • How would designers interact with this code?
// In ideal world, designers shouldn't interact with code. If we're talking about art team, it should not be bothered with code
// or other implementations of Unity
// if we're talking about game designers, they should be able to use editor tools to modify game data
// and config system should be written to be able to load configs from buckets, addressables or Google Tables

// • How would you profile or debug performance issues?
// First of all, using tools provided - Unity Profiler, Frame Debugger (for render purposes), Rider Profiler and debug mode
// but good logging could help us find the root cause of the issue in runtime too
// then, find those bottlenecks and do whatever is needed, to fix or optimize enough

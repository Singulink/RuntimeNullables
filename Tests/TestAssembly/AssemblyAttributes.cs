using RuntimeNullables;

// These don't change anything (null checks are on by default), but the weaver has to read and remove them like any other [NullChecks] attribute.

[assembly: NullChecks(true)]
[module: NullChecks(true)]

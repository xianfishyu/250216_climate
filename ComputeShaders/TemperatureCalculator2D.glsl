

#[compute]

#version 450

layout(local_size_x=64,local_size_y=64,local_size_z=1)in;

layout(set=0,binding=0,std430)buffer LocalTemp{
    float data[];
}local_temp;

layout(set=0,binding=1,std430)buffer NeighborIndex{
    uvec4 data[];
}neighbor_index;

void main()
{
    uint x=gl_GlobalInvocationID.x;
    uint y=gl_GlobalInvocationID.y;
    uint z=gl_GlobalInvocationID.z;
    
    uint id=x+y*gl_NumWorkGroups.x+z*gl_NumWorkGroups.x*gl_NumWorkGroups.y;
    
    float deltaT=0.;
    
    deltaT+=(local_temp.data[neighbor_index.data[id].x]-local_temp.data[id])*.1;
    deltaT+=(local_temp.data[neighbor_index.data[id].y]-local_temp.data[id])*.1;
    deltaT+=(local_temp.data[neighbor_index.data[id].z]-local_temp.data[id])*.1;
    deltaT+=(local_temp.data[neighbor_index.data[id].w]-local_temp.data[id])*.1;

    local_temp.data[id] += deltaT;
}

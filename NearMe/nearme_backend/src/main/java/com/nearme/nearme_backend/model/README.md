These are data structures we want to send through the RESTful services

They are all pretty simple structures, just getter, setter, toString, and fields. They are generated using an extension `java code generators`

They are the POJO classes, in Spring boot I think they are called DTOs (Data Transfer Objects).

I searched a lot but didn't get very definitive answers, but most of the tutorials seems to say Spring can handle non-basic fields (like the Location class) inside an object, as long as they are "serializable" which is also unexplained either by example or by giving exact requirement.

Some of the request and response are pretty simple (only one field or is just a confirmation), in such case we use simple direct basic types.

### NOTE:

1. Force all geo coordinate array/param sequence to follow lat, lng, alt order
2. Force all color array/param sequence to follow Red, Green, Blue order
3. Force all size array/param to follow height, width order

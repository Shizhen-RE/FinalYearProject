# Table of Contents

* [Root folder for backend ({FYDP_PROJECT_ROOT}/NearMe/nearme_backend)](#rootfolder)
    * [pom file](#pom)
    * [mvnw file](#mvnw)
* [Source code folder ({...nearme_backend/src/main/java/.../nearme_backend})](#sourcefolder)
    * [controller](#controller)
    * [dao](#dao)
    * [model](#model)
    * [service](#service)
    * [How it is run by Spring](#running)
* [Test folder (...nearme_backend/src/main/java/.../nearme_backend)](#testfolder)
    * [Writing a test](#writetest)
    * [How to test](#testing)
* [Useful links](#links)

# <a name="rootfolder"></a>Root folder for backend

Where: {FYDP_PROJECT_ROOT}/NearMe/nearme_backend

Under the project root folder, there are three folders:

* .mvn folder: Not managed by us. Maven created it for its job so don't touch it.
* src folder: all the source code we write should go into this folder.
* target folder: this folder is the compile and build result of the code we wrote in src folder. Nothing we really need to worry about here except the output .jar file. This folder is gitignored.

    The file is named in the following structure: `nearme_backend-X.X.X-SNAPSHOT.jar`. This file is what we can safely copy out to other machines with appropriate java runtime and run with java command.

There are 3 files directly under this directory:

## <a name = "pom"></a>pom.xml

pom.xml describes the project metadatas and dependencies. It's purpose is similar to `package.json` in node.js or `requirements.txt` in python, `CMakeList.txt` in C/C++. You can find in there:

* The name, version we want our project to be (which appears in the .jar file name)
* The version of springboot, java we are running.
* Third party dependencies and their versions
* Other less known propeties, update them per package documentation request.

This pom file is read by maven when you try to build the project and maven will manage downloading installing of these dependencies. Our current dependency installed are:

* springboot families.
* google cloud API (for firebase auth)
* mongodb API (for database)
* other implicit dependencies (mostly from springboot root).

## <a name = "mvnw"></a>mvnw

These files are from maven, sets something for mvn builds. There are two files, and `mvnw.cmd` is just the same functionality of `mvnw` for the Windows. We don't manage them on our own so better leave it alone.

When building the project, run:

`mvn clean install`

Maven will build the project and run the test cases. A jar will be created in target folder, which is then run with `java XXX.jar` to start the server.

When developing test scripts, you might want to temporarily skip tests as they are faulty, but still get the built jar file. In this case, use `mvn clean install -DskipTests`, then the test result won't affect the end output.

More detailed maven build can be found here: https://maven.apache.org/guides/introduction/introduction-to-the-lifecycle.html if further customization is needed for containerization etc.

# <a name = "sourcefolder"></a>Source code folder

The source code folder contains all the running code for the backend. It is build pretty much following standard spring backend structure.

The Application class (`NearmeBackendApplication.java`, hosts the main function) is at the root. Its purpose is mostly init, and start the server. Some one-time setup code can go here.

It is marked by `@SpringBootApplication` so Spring knows what it is.

## <a name = "controller"></a>controller

Files inside the controller folder are for receiving and understanding the HTTP requests received and pushing to proper service. The member functions of these classes just do the following work:

* What URL and what HTTP function (GET/SET/...) it is listening at. Specified by marking like: `@GetMapping(value = "/getTopicList")`
* What params are looking for (a specific format body, a header named something), and how are they passed in (RequestParam (url inline)? Body? Header?) each have different markings but you can find example in current code.
* Check the auth for an user identification (Note this can be left for the service to do. It is just a random decision here)
* Do maybe some small processing and send to appropriate function and service.
* Return status and return value

Note that they have private member variables of the service they need to talk to.

## <a name = "dao"></a>dao

Files inside this folder concerns how to access database. Currently it is a single accessor but future if multiple database or balancing load or distributed accessing rules might make it complex.

Currently, database accessor allows search with ID for all tables. And some search with column for ID since they are randomly needed and is there upon programming need.

## <a name = "model"></a>model

Files inside this folder are different body structures we use in different APIs. The objects should be treated as POJOs (Plain Old Java Objects), and marked with `@JsonNaming(PropertyNamingStrategies.UpperCamelCaseStrategy.class)` as our json fields are named that way. This tells Spring how to map the json into a java object and vice versa.

The current naming convention of these classes is: the API the model belongs to + it is for the request or response, written in UpperCamelCase as Java convention. As per standard, the class should have constructor with empty and all param, setters, getters. I see some online guides seem to have a thing to help skip writing these methods.

## <a name = "service"></a>service

Service is where the backend logic takes place. It takes inputs from the controller, then mostly go to different backend resources for different logic steps they need to go through and finally returns. Apart from it must jointly be able to provide service to every single possible API call, not much strict format is placed on them.

The interface - implementation is preparing for if we future go with different, say, authentication provider. It is not necessary as a small project.

## <a name = "running"></a> How it is run by Spring

Entry point of the system is the main function (`NearmeBackendApplication.java`). The main method is run as normal all the way to the `.run` function. After that the management of the server is handled by Spring and hidden from us.

When a request comes in, It scans all the classes marked by `@RestController` to try find a mapping function listening to that URL, restructure the request (for example, JSON body -> POJO class specified) and call the controller function.

Starting from controller function, we have full control as we are not using any more advanced features of Spring Boot. We then call the service function to provide what we want.

Service function goes to different resources for the request. Currently, it mostly goes to database accessor. After everything is done, it packs the result and return to controller.

As long as nothing went wrong, controller just hands what service returned to Spring with the status code OK. Spring can understand: array of objects, list of objects, objects inside objects. Then it is parsed to JSON and respond the request.

# <a name = "testfolder"></a>Test folder

The general expectation of Test folder is for it to structure in the same way as the source code folder. It can have an additional folder for testutils. The naming convention of test code files is `<class under test>Test.java`. The test would use Mockito to mock its calls to other function so it is completely unit test to prove the function work as expected.

There could be a seperate folder for integration test, subject to whether we want to do it.

## <a name = "writetest"></a>How to write a test

Not much is done there so no need to follow this guide closely.

First, find or create the test java file which is located exactly the same way as the source folder file structure.

Then, mark this file as a test. For API test, use `@WebMvcTest(<file under test>.class)`. Mention to use the Spring's stuff `@Autowired MockMvc mockMvc;`, and use `@MockBean` to mock any other classes this class is using a function from.

Finally, write the test case for your specific function. use standard JUnit markings like `@Before` `@Test` to indicate what the function is. When needed, use Mockito to mock function calls and check whether it matches expectation.

## <a name = "testing"></a> How to test

For running a specific test case, simply use your IDE's test button to run it, or if you prefer the fancy long line of command. The tests are standard java unit tests anyway.

The build command `mvn clean install` itself includes the step of running through all tests and report result in console. You can check the official website for the different stage of maven build and how to use them.

# <a name = "links"></a>Useful Links

The project is started and developed following this guide up until this point: https://www.tutorialspoint.com/spring_boot/spring_boot_building_restful_web_services.htm Check left panel for different parts of the guide.

The current only API test code was following this guide: https://stackabuse.com/guide-to-unit-testing-spring-boot-rest-apis/ Which, compared to the above guide, is doing full seperation of different layers by mocking.

Maven's official document: https://maven.apache.org/guides/getting-started/index.html

Spring's document: https://docs.spring.io/spring-framework/docs/current/reference/html/ Which is too big to swallow. Further focus on the following part:

* https://spring.io/guides/gs/rest-service/
* https://docs.spring.io/spring-framework/docs/current/reference/html/testing.html#mock-objects-servlet
* https://docs.spring.io/spring-framework/docs/current/reference/html/web.html#spring-web
* https://spring.io/guides To try find the thing to look for.

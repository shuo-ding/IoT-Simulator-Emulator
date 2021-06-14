# IoT Simulator - v2.0  
                                Author Dr Shuo Ding  14 June 2021
                                
----- ASP.NET Core C#, simulate unlimited IoT sensors (Static and Vehicle) for testing big IoT data sending and receiving (MQTT messages) in real-time.

 <img src="https://iotnextday.com/images/dashboardmetal.jpg" alt="IoT Simulator">
 
 The Message Queuing Telemetry Transport (MQTT) is a lightweight, publish-subscribe network protocol that transports messages between IoT devices. No matter what types of underlayer radio technologies are, on top of the TCP/IP stack and an encrypted TLS connection possibly, MQTT (Message Queue Telemetry Transport) has become the standard for IoT communications in reality. There exist a message broker and a number of clients in the MQTT network. The broker is a server that buffers all messages published by clients under a topic and sends the subscribed messages to those subscribing clients.  

IoT Simulator is a realtime IoT sensor simulator based on MQTT protocol, with unlimited capacity of sending MQTT messages to the MQTT broker, per user defined. The simulator can be configured as "Sender", "Receiver", or "Transceiver" role as user desired. Simulation parameters can be easily configured by XML file, and simulation results will be stored as TXT files with timestamp, and inserted into SQLServer database. A Power BI visualization dashboard is also provided to visualize the realtime statistics. In 
IoT Simulator v2.0, two types of sensors are supported, Static and Vehicle. Through selecting route on Google map and converting to GPX file, IoT Simulator is able to simulate vehicles moving on GPX map, with varying speed, and pause time. The Static sensors will send messages from static positions, simulating Temperature, Air Quality, and Humidity sensors. 

IoT Simulator v2.0 provides you with a great platform for developing applications with the requirement of simulating unlimited IoT sensors in your project, based on a mature MQTT communication library, M2MQTT. Some use cases may include: IoT network attacking, vehicle real-time location and guidance in smart car park scenario, smart traffic lights and electrical signs/road sensors for autonomous vehicles, etc.

Please visit my documentation website 

<a href="https://iotnextday.com" style="font-size:19px">IoT Next Day</a>
 
 


# IoT Simulator / Emulator  - v2.0  
                                Author Dr Shuo Ding  14 June 2021
                                
----- ASP.NET Core C#, simulate unlimited virtual IoT sensors (Static and Vehicle) and emulate sending and receiving real IoT MQTT messages to Internet in real-time.

 <img src="https://iotnextday.com/images/dashboardmetal.jpg" alt="IoT Simulator">
 
The Message Queuing Telemetry Transport (MQTT) is a lightweight, publish-subscribe network protocol that transports messages between IoT devices. No matter what types of underlayer radio technologies are, on top of the TCP/IP stack and an encrypted TLS connection possibly, MQTT (Message Queue Telemetry Transport) has become the standard for IoT communications in reality. There exist a message broker and a number of clients in the MQTT network. The broker is a server that buffers all messages published by clients under a topic and sends the subscribed messages to those subscribing clients.  

The IoT Simulator/Emulator is based on MQTT protocol, with unlimited capacity to simulate virtual IoT sensors and emulate sending/receiving real MQTT messages to MQTT broker on Internet, per user defined. It can be configured as "Sender", "Receiver", or "Transceiver" role as user desired. Simulation parameters can be easily configured by XML file, and simulation results will be stored as TXT files with timestamp, and inserted into SQLServer database. A Power BI visualization dashboard is also provided to visualize the realtime statistics. In IoT Simulator/Emulator v2.0, two types of sensors are supported, Static and Vehicle. Through selecting route on Google map and converting to GPX file, IoT Simulator/Emulator v2.0 is able to simulate vehicles moving on GPX map, with varying speed, and pause time. The Static sensors will send messages from static positions, simulating Temperature, Air Quality, and Humidity sensors. 

IoT Simulator/Emulator v2.0 provides you with a great platform for developing applications with the requirement of simulating unlimited virtual IoT sensors in your project, based on a mature MQTT communication library, M2MQTT. Some use cases may include: IoT network attacking, vehicle real-time location and guidance in smart car park scenario, smart traffic lights and electrical signs/road sensors for autonomous vehicles, etc.

Please visit my documentation website 

<a href="https://iotnextday.com" style="font-size:19px">IoT Next Day</a>
 
If you are not able to access the website from some locations of the world, you can email me: Shuo.Ding.Australia@Gmail.com to solve this. 
 


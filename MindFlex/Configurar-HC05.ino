#include <SoftwareSerial.h>

SoftwareSerial HC05(11, 10); // RX, TX
void setup()
{
  Serial.begin(9600);
  while (!Serial)
  {
  }
  Serial.println("CONNECTION INITIALIZED!");
  mySerial.begin(38400);
}

void loop()
{
  if (HC05.available())
  {
    Serial.write(HC05.read());
  }
  if (Serial.available())
  {
    HC05.write(Serial.read());
  }
}
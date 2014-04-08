#pragma once

#include "ISerializable.h"
#include "TAntiCollisionQueue.h"
#include "Group.h"
#include "Teacher.h"
#include "IRoom.h"
#include <ctime>

using namespace std;

class Teacher;
class IRoom;
class Group;

class Reservation : public ISerializable
{
public:
	string Name;
	time_t DateOfStart;
	time_t DateOfEnd;
	Teacher& BoundTeacher;
	TAntiCollisionQueue<Group> BoundGroups;
	IRoom& Room;


	Reservation(string _name, time_t _dateOfStart, time_t _dateOfEnd, Teacher& _teacher, IRoom& _room)
		: BoundTeacher(_teacher), Room(_room)
	{
		Name = _name;
		DateOfStart = _dateOfStart;
		DateOfEnd = _dateOfEnd;
		cout << ctime(&DateOfStart) << endl;
		cout << ctime(&DateOfEnd);
	}

	void Serialize(ostream& os) const
	{
		ISerializable::Serialize(os);
		os << " " << Name << " "
			<< ctime(&DateOfStart) << " " << ctime(&DateOfEnd)
			<< BoundTeacher.Id << " " << Room.Id;
	}
};
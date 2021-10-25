
Declare @DateTime as Datetime = '2021-10-21 00:00:01'

select * from [dbo].[BotDataEntity]
where TimeStamp  >= @DateTime

select * from BotReplies
where CreationDate >=  @DateTime

select * from [dbo].[Messages]
where CreationDate >= @DateTime

select * from [dbo].[Requests]
where CreationDate >= @DateTime

----Fetch UserList To Delete
--select u.UserId from [dbo].[Users] u where userid in
--(
--select RequesterId from [dbo].[Requests]
--where CreationDate >=  @DateTime
--Union
--select AgentId from [dbo].[Requests]
--where CreationDate >=  @DateTime
--)

--select u.UserId from [dbo].[Users] u where userid in
--(
--select FromId from [dbo].[Messages]
--where CreationDate >= @DateTime
--)

--select u.UserId from [dbo].[Users] u where userid in
--(
--select ToId from BotReplies
--where CreationDate >=  @DateTime
--)

-- Delete Data

Delete from [dbo].[BotDataEntity]
where TimeStamp  >= @DateTime

Delete from BotReplies
where CreationDate >=  @DateTime

Delete from [dbo].[Messages]
where CreationDate >= @DateTime

Delete from [dbo].[Requests]
where CreationDate >= @DateTime

delete from Users
where UserId in 
(69,
115,
138,
75,
109,
89,
95,
72,
118,
66,
78,
129,
106,
86,
135,
63,
112,
6,
98,
144,
113,
67,
127,
81,
130,
87,
101,
70,
141,
93,
84,
110,
133,
61,
90,
104,
4,
119,
96,
142,
65,
79,
73,
105,
85,
136,
99,
148,
139,
122,
128,
76,
108,
82,
88,
102,
145,
71,
94,
140,
77,
123,
83,
134,
91,
103,
97,
143,
120,
114,
68,
80,
74,
131,
137,
100,
4,
63,
66,
112,
131,
133,
141,
142,
145,
4,
63,
65,
66,
67,
68,
69,
70,
71,
72,
73,
74,
75,
76,
77,
78,
82,
83,
87,
89,
91,
94,
95,
97,
100,
102,
104,
106,
108,
109,
110,
112,
129,
130,
131,
133,
134,
135,
136,
137,
138,
140,
141,
142,
144,
145,
148)
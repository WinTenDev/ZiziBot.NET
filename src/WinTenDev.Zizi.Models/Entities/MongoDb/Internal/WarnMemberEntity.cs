﻿using System;
using MongoDB.Bson;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

[Collection("WarnMember")]
public class WarnMemberEntity : IEntity, ICreatedOn
{
    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();

    public string ID { get; set; }
    public long ChatId { get; set; }
    public long MemberUserId { get; set; }
    public string MemberFirstName { get; set; }
    public string MemberLastName { get; set; }
    public long AdminUserId { get; set; }
    public string AdminFirstName { get; set; }
    public string AdminLastName { get; set; }
    public string Reason { get; set; }
    public DateTime CreatedOn { get; set; }
}
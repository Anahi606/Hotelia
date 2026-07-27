using System;
using System.Collections.Generic;

[Serializable]
public class AIQuestionParametersListData
{
    public List<AIQuestionParametersData> parameters = new List<AIQuestionParametersData>();
}

[Serializable]
public class AIQuestionParametersData
{
    public string parameterId;

    public string courseId;
    public string subjectCode;
    public string subjectName;
    public string classCode;
    public string teacherPlayFabId;

    public string scenarioParameters;
    public string focusInstructions;
    public string questionGoal;
    public string allowedTopicsCsv;
    public string correctKeywordsCsv;
    public string wrongKeywordsCsv;

    public string npcRole;
    public string answerLanguage;

    public string status;
    public string updatedUtc;
}

[Serializable]
public class ClassAIRegistryListData
{
    public List<ClassAIRegistryItemData> classes = new List<ClassAIRegistryItemData>();
}

[Serializable]
public class ClassAIRegistryItemData
{
    public string classCode;
    public string teacherPlayFabId;
    public string courseId;
    public string subjectCode;
    public string subjectName;
    public string status;
}

[Serializable]
public class SaveAIQuestionParametersRequestData
{
    public string sessionTicket;
    public string parameterId;

    public string courseId;
    public string subjectCode;
    public string subjectName;
    public string classCode;

    public string scenarioParameters;
    public string focusInstructions;
    public string questionGoal;
    public string allowedTopicsCsv;
    public string correctKeywordsCsv;
    public string wrongKeywordsCsv;

    public string npcRole;
    public string answerLanguage;
}

[Serializable]
public class DeleteAIQuestionParametersRequestData
{
    public string sessionTicket;
    public string parameterId;
}

[Serializable]
public class DeleteAIQuestionParametersResponseData
{
    public bool success;
    public string message;
    public string parameterId;
}

[Serializable]
public class SaveAIQuestionParametersResponseData
{
    public bool success;
    public string message;
    public AIQuestionParametersData parameters;
}

[Serializable]
public class GetAIQuestionParametersForStudentRequestData
{
    public string sessionTicket;
    public string classCode;
}

[Serializable]
public class GetAIQuestionParametersForStudentResponseData
{
    public bool success;
    public string message;
    public AIQuestionParametersData parameters;
}
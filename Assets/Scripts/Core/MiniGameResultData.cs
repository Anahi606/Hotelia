using System;

[Serializable]
public class MiniGameResultData
{
    public int day;
    public string minigameName;

    public int satisfaction;
    public int revenue;
    public int errors;
    public int timeScore;
    public int finalScore;

    public string stpSummary;
    public string feedback;

    public int clientBudget;
    public int packageCost;

    public GuestSegment selectedSegment;
    public OfferType selectedOffer;
    public TourismExtraType selectedTourismExtra;

    public bool hasDetailedCheckInScores;

    public bool roomCorrect;
    public bool segmentCorrect;
    public bool offerCorrect;
    public bool tourismExtraCorrect;
    public bool budgetCorrect;

    public int roomScore;
    public int stpScore;
    public int offerScore;
    public int tourismExtraScore;
    public int budgetScore;
}
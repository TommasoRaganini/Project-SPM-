namespace Domain.States;

public class OpenParticipationState : IParticipationState
{
    public ParticipationStatus Status => ParticipationStatus.Open;
    public bool CanAcceptProposals => true;
}


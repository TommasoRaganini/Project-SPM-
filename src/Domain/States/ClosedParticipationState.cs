namespace Domain.States;

public class ClosedParticipationState : IParticipationState
{
    public ParticipationStatus Status => ParticipationStatus.Closed;
    public bool CanAcceptProposals => false;
}


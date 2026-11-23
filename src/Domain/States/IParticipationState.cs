namespace Domain.States;

public interface IParticipationState
{
    ParticipationStatus Status { get; }
    bool CanAcceptProposals { get; }
}


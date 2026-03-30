import { useMutation, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import type { GameNightRsvpState } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { updateGameNightRsvpCall } from "@/services/gameNightService";
import { getGameNightByLink } from "@/services/queries/gameNights";

export const useRsvpData = (linkId: string) => {
	const { errorToast } = useToasts();
	const [isSubmitted, setIsSubmitted] = useState(false);
	const [submittedPlayerName, setSubmittedPlayerName] = useState("");
	const [submittedState, setSubmittedState] = useState<GameNightRsvpState | null>(null);

	const { data: gameNight, isLoading } = useQuery(getGameNightByLink(linkId));

	const rsvpMutation = useMutation({
		mutationFn: updateGameNightRsvpCall,
		onSuccess: () => {
			setIsSubmitted(true);
		},
		onError: () => {
			errorToast("rsvp:rsvp-failed");
		},
	});

	const submitRsvp = (rsvpId: number, playerId: number, playerName: string, state: GameNightRsvpState) => {
		if (!gameNight) return;
		setSubmittedPlayerName(playerName);
		setSubmittedState(state);
		rsvpMutation.mutate({
			id: rsvpId,
			gameNightId: gameNight.id,
			playerId,
			state,
		});
	};

	return {
		gameNight,
		isLoading,
		submitRsvp,
		isSubmitting: rsvpMutation.isPending,
		isSubmitted,
		submittedPlayerName,
		submittedState,
	};
};

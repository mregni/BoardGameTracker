import { useMutation, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import type { GameNightRsvpState, UpdateGameNightRsvp } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { updateGameNightRsvpByLinkCall } from "@/services/gameNightService";
import { getGameNightByLink } from "@/services/queries/gameNights";
import { getGameNightManuals } from "@/services/queries/manuals";

export const useRsvpData = (linkId: string) => {
	const { errorToast } = useToasts();
	const [isSubmitted, setIsSubmitted] = useState(false);
	const [submittedPlayerName, setSubmittedPlayerName] = useState("");
	const [submittedPlayerId, setSubmittedPlayerId] = useState<number | null>(null);
	const [submittedState, setSubmittedState] = useState<GameNightRsvpState | null>(null);

	const { data: gameNight, isLoading, refetch } = useQuery(getGameNightByLink(linkId));
	const { data: manuals } = useQuery(getGameNightManuals(linkId));

	const rsvpMutation = useMutation({
		mutationFn: (rsvp: UpdateGameNightRsvp) => updateGameNightRsvpByLinkCall(linkId, rsvp),
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
		setSubmittedPlayerId(playerId);
		setSubmittedState(state);
		rsvpMutation.mutate({
			id: rsvpId,
			gameNightId: gameNight.id,
			playerId,
			state,
		});
	};

	const changeResponse = () => {
		setIsSubmitted(false);
		refetch();
	};

	return {
		gameNight,
		changeResponse,
		submittedPlayerId,
		manuals: manuals ?? [],
		isLoading,
		submitRsvp,
		isSubmitting: rsvpMutation.isPending,
		isSubmitted,
		submittedPlayerName,
		submittedState,
	};
};

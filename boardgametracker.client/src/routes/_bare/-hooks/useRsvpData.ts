import { useMutation, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { type GameNightRsvpState, type GameNightRsvps, isApiError, type UpdateGameNightRsvp } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { updateGameNightRsvpByLinkCall } from "@/services/gameNightService";
import { getGameNightByLink } from "@/services/queries/gameNights";
import { getGameNightManuals } from "@/services/queries/manuals";
import { apiErrorMessage } from "@/utils/errorUtils";

export interface RsvpAnswer {
	playerId: number;
	state: GameNightRsvpState;
	hostNotified: boolean;
}

export const useRsvpData = (linkId: string) => {
	const { errorToast } = useToasts();
	const [answer, setAnswer] = useState<RsvpAnswer | null>(null);
	const [isEditing, setIsEditing] = useState(true);

	const { data: gameNight, isLoading, error, refetch } = useQuery(getGameNightByLink(linkId));
	const { data: manuals } = useQuery(getGameNightManuals(linkId));

	const rsvpMutation = useMutation({
		mutationFn: (rsvp: UpdateGameNightRsvp) => updateGameNightRsvpByLinkCall(linkId, rsvp),
		onSuccess: (data, rsvp) => {
			setAnswer({ playerId: rsvp.playerId, state: rsvp.state, hostNotified: data.hostNotified });
			setIsEditing(false);
			void refetch();
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "rsvp:rsvp-failed"));
		},
	});

	const submitRsvp = (rsvp: GameNightRsvps, state: GameNightRsvpState) => {
		rsvpMutation.mutate({ id: rsvp.id, gameNightId: rsvp.gameNightId, playerId: rsvp.playerId, state });
	};

	return {
		gameNight,
		manuals: manuals ?? [],
		isLoading,
		isNotFound: !linkId || (isApiError(error) && error.status === 404),
		error,
		retry: () => void refetch(),
		answer,
		isEditing,
		submitRsvp,
		isSubmitting: rsvpMutation.isPending,
		changeResponse: () => setIsEditing(true),
	};
};

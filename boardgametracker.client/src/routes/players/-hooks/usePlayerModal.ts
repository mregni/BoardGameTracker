import { useMutation, useQueryClient } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { uploadImageCall } from "@/services/imageService";
import { addPlayerCall, linkPlayerAccountCall, updatePlayerCall } from "@/services/playerService";
import { apiErrorMessage } from "@/utils/errorUtils";

interface Props {
	onSaveSuccess?: () => void;
	onUpdateSuccess?: () => void;
}

export const usePlayerModal = ({ onSaveSuccess, onUpdateSuccess }: Props) => {
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();

	const uploadImageMutation = useMutation({
		mutationFn: uploadImageCall,
	});

	const saveMutation = useMutation({
		mutationFn: addPlayerCall,
		async onSuccess() {
			successToast("player:notifications.created");
			onSaveSuccess?.();
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.players] });
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.dashboard] });
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "player:notifications.create-failed"));
		},
	});

	const updateMutation = useMutation({
		mutationFn: updatePlayerCall,
		async onSuccess() {
			successToast("player:notifications.updated");
			onUpdateSuccess?.();
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.players] });
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.player] });
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "player:notifications.update-failed"));
		},
	});

	const linkAccountMutation = useMutation({
		mutationFn: ({ playerId, userId }: { playerId: number; userId: string | null }) =>
			linkPlayerAccountCall(playerId, userId),
		async onSuccess() {
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.players] });
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.player] });
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.users] });
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.profile] });
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "player:account.link-failed"));
		},
	});

	const isLoading =
		uploadImageMutation.isPending ||
		saveMutation.isPending ||
		updateMutation.isPending ||
		linkAccountMutation.isPending;

	return {
		savePlayer: saveMutation.mutateAsync,
		updatePlayer: updateMutation.mutateAsync,
		uploadImage: uploadImageMutation.mutateAsync,
		linkAccount: linkAccountMutation.mutateAsync,
		isLoading,
	};
};

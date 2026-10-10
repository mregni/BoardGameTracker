import { useMutation, useQueryClient } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { uploadImageCall } from "@/services/imageService";
import { addPlayerCall, updatePlayerCall } from "@/services/playerService";
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

	const isLoading = uploadImageMutation.isPending || saveMutation.isPending || updateMutation.isPending;

	return {
		savePlayer: saveMutation.mutateAsync,
		updatePlayer: updateMutation.mutateAsync,
		uploadImage: uploadImageMutation.mutateAsync,
		isLoading,
	};
};

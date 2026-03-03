import { useMutation, useQueryClient } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { addLocationCall, updateLocationCall } from "@/services/locationService";

interface Props {
	onSaveSuccess?: () => void;
	onUpdateSuccess?: () => void;
}

export const useLocationModal = ({ onSaveSuccess, onUpdateSuccess }: Props) => {
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();

	const saveMutation = useMutation({
		mutationFn: addLocationCall,
		async onSuccess() {
			successToast("location.notifications.created");
			onSaveSuccess?.();
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.locations] });
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
		},
		onError: () => {
			errorToast("location.notifications.create-failed");
		},
	});

	const updateMutation = useMutation({
		mutationFn: updateLocationCall,
		async onSuccess() {
			successToast("location.notifications.update");
			onUpdateSuccess?.();
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.locations] });
		},
		onError: () => {
			errorToast("location.notifications.update-failed");
		},
	});

	const isLoading = saveMutation.isPending || updateMutation.isPending;

	return {
		saveLocation: saveMutation.mutateAsync,
		updateLocation: updateMutation.mutateAsync,
		isLoading,
	};
};

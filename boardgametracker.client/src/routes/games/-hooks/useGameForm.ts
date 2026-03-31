import { useMutation, useQueries } from "@tanstack/react-query";
import { uploadImageCall } from "@/services/imageService";
import { getSettings } from "@/services/queries/settings";

export const useGameForm = () => {
	const [settingsQuery] = useQueries({
		queries: [getSettings()],
	});

	const settings = settingsQuery.data;

	const uploadImageMutation = useMutation({
		mutationFn: uploadImageCall,
	});

	return {
		isLoading: settingsQuery.isLoading || uploadImageMutation.isPending,
		uploadImage: uploadImageMutation.mutateAsync,
		settings,
	};
};

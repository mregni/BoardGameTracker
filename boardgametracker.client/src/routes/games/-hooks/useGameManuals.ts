import { useMutation, useQuery } from "@tanstack/react-query";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import { useToasts } from "@/routes/-hooks/useToasts";
import { deleteManualCall, downloadManualCall, uploadManualsCall } from "@/services/manualService";
import { getGameManuals } from "@/services/queries/manuals";

export const useGameManuals = (gameId: number) => {
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();

	const { data: manuals, isLoading } = useQuery(getGameManuals(gameId));

	const uploadMutation = useMutation({
		mutationFn: (files: File[]) => uploadManualsCall(gameId, files),
		onSuccess: async () => {
			await invalidator.invalidateGame(gameId);
			successToast("game:manuals.upload-success");
		},
		onError: () => {
			errorToast("game:manuals.upload-failed");
		},
	});

	const deleteMutation = useMutation({
		mutationFn: deleteManualCall,
		onSuccess: async () => {
			await invalidator.invalidateGame(gameId);
			successToast("game:manuals.delete-success");
		},
		onError: () => {
			errorToast("game:manuals.delete-failed");
		},
	});

	const downloadManual = async (id: number, title: string) => {
		try {
			await downloadManualCall(id, title);
		} catch {
			errorToast("game:manuals.download-failed");
		}
	};

	return {
		manuals,
		isLoading,
		uploadManuals: uploadMutation.mutate,
		isUploading: uploadMutation.isPending,
		deleteManual: deleteMutation.mutate,
		downloadManual,
	};
};

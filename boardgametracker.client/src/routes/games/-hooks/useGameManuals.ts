import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRef } from "react";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import { type GameManual, QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { deleteManualCall, downloadManualCall, reindexManualCall, uploadManualsCall } from "@/services/manualService";
import { getGameManuals } from "@/services/queries/manuals";

const POLL_INTERVAL_MS = 3000;
const MAX_STATUS_POLLS = 60;

const isIndexingInProgress = (manuals: GameManual[] | undefined): boolean =>
	(manuals ?? []).some((manual) => manual.indexStatus === "pending" || manual.indexStatus === "indexing");

export const useGameManuals = (gameId: number, pollWhileIndexing = false) => {
	const invalidator = useQueryInvalidator();
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();
	const pollCountRef = useRef(0);

	const { data: manuals, isLoading } = useQuery({
		...getGameManuals(gameId),
		refetchInterval: (query) => {
			if (!pollWhileIndexing || !isIndexingInProgress(query.state.data)) {
				pollCountRef.current = 0;
				return false;
			}
			if (pollCountRef.current >= MAX_STATUS_POLLS) {
				return false;
			}
			pollCountRef.current += 1;
			return POLL_INTERVAL_MS;
		},
	});

	const invalidateManuals = () =>
		queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.game, gameId, QUERY_KEYS.manuals] });

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

	const reindexMutation = useMutation({
		mutationFn: reindexManualCall,
		onSuccess: async () => {
			pollCountRef.current = 0;
			await invalidateManuals();
			successToast("game:manuals.reindex-success");
		},
		onError: () => {
			errorToast("game:manuals.reindex-failed");
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
		reindexManual: reindexMutation.mutate,
		isReindexing: reindexMutation.isPending,
		downloadManual,
	};
};

import { useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";

import { QueryInvalidator } from "@/services/queries/invalidations";

export const useQueryInvalidator = () => {
	const queryClient = useQueryClient();
	return useMemo(() => new QueryInvalidator(queryClient), [queryClient]);
};

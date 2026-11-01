import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import AlertTriangle from "@/assets/icons/alert-triangle.svg?react";
import { isApiError } from "@/models";
import { BgtEmptyState } from "../BgtLayout/BgtEmptyState";
import { BgtLoadingSpinner } from "../BgtLoadingSpinner/BgtLoadingSpinner";

type NonUndefined<T> = T extends undefined ? never : T;
type RequiredData<T> = { [K in keyof T]: NonUndefined<T[K]> };

interface DataGuardProps<T extends Record<string, unknown>> {
	isLoading: boolean;
	data: T;
	children: (data: RequiredData<T>) => ReactNode;
	fallback?: ReactNode;
	error?: unknown;
	onRetry?: () => void;
	notFound?: ReactNode;
}

const errorMessageKey = (error: unknown) => {
	if (!isApiError(error)) return "something-went-wrong";
	if (error.kind === "network" || error.kind === "timeout" || error.kind === "server") return error.kind;
	return "something-went-wrong";
};

const LoadError = ({ error, onRetry }: { error: unknown; onRetry?: () => void }) => {
	const { t } = useTranslation(["error", "common"]);

	return (
		<BgtEmptyState
			icon={AlertTriangle}
			title={t("load-failed")}
			description={t(errorMessageKey(error))}
			action={onRetry && { label: t("common:try-again"), onClick: onRetry }}
		/>
	);
};

/**
 * A guard component that handles loading states and ensures all data is defined
 * before rendering children. This eliminates the need for verbose undefined checks.
 *
 * @example
 * ```tsx
 * <BgtDataGuard
 *   isLoading={isLoading}
 *   data={{ player, statistics, badges, settings }}
 * >
 *   {({ player, statistics, badges, settings }) => (
 *     <div>
 *       <h1>{player.name}</h1>
 *       <p>{statistics.playCount} plays</p>
 *     </div>
 *   )}
 * </BgtDataGuard>
 * ```
 */
export const BgtDataGuard = <T extends Record<string, unknown>>({
	isLoading,
	data,
	children,
	fallback = <BgtLoadingSpinner />,
	error,
	onRetry,
	notFound,
}: DataGuardProps<T>) => {
	// Check if loading
	if (isLoading) {
		return <>{fallback}</>;
	}

	// Check if any data property is undefined
	const hasUndefinedData = Object.values(data).includes(undefined);

	if (hasUndefinedData) {
		if (error && notFound && isApiError(error) && error.status === 404) return <>{notFound}</>;
		if (error) return <LoadError error={error} onRetry={onRetry} />;
		return <>{fallback}</>;
	}

	// All data is defined, safe to render children
	// Type assertion is safe here because we've checked all values are defined
	return <>{children(data as RequiredData<T>)}</>;
};

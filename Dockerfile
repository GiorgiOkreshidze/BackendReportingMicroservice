# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Set the working directory
WORKDIR /app

# Copy the published app from the build stage
COPY /app ./

# Expose port 80
EXPOSE 80

# Run the application
ENTRYPOINT ["dotnet", "Reporting.API.dll"]
